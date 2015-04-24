using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Ionic.Zip;
using Newtonsoft.Json;

namespace HotAssembly
{
    public class InstantiatorFactory<T>
    {
        public delegate T Instantiator<T>(params object[] args);

        private readonly IPersistenceProvider _persistenceProvider;

        public static Instantiator<T> GetInstantiator<T> (ConstructorInfo ctor)
        {
            var type = ctor.DeclaringType;
            var paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            var param =
                Expression.Parameter(typeof (object[]), "args");

            var argsExp =
                new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (var i = 0; i < paramsInfo.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = paramsInfo[i].ParameterType;

                var paramAccessorExp =
                    Expression.ArrayIndex(param, index);

                var paramCastExp =
                    Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            var newExp = Expression.New(ctor, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
//            var lambda = Expression.Lambda<Instantiator<T>>(Expression.Convert(newExp, typeof(T)), param);
            var lambda = Expression.Lambda<Instantiator<T>>(newExp, param);

            //compile it
            var compiled = (Instantiator<T>) lambda.Compile();
            return compiled;
        }

        private static ConcurrentDictionary<string, Instantiator<T>> _instantiators;
        private static ConcurrentDictionary<string, Instantiator<T>> Instantiators
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _instantiators,
                    () => new ConcurrentDictionary<string, Instantiator<T>>());
                return _instantiators;
            }
        }

        public InstantiatorFactory(IPersistenceProvider persistenceProvider)
        {
            _persistenceProvider = persistenceProvider;
        }

        /// <summary>
        /// Creates an instance of a requested class
        /// </summary>
        /// <typeparam name="T">type of the interface or a base class to instantiate</typeparam>
        /// <param name="bundleId">id of the assembly to instantiate.</param>
        /// <param name="data"></param>
        /// <returns></returns>
        public T Instantiate(string bundleId, object data)
        {
            if (bundleId.Contains('/'))
                throw new InstantiatorException("Buldle Id should not contain forward slashes", null); 

            Instantiator<T> instantiator;
            Exception instantiatorException;

            if (Instantiators.TryGetValue(bundleId, out instantiator)) 
                return instantiator(data);
            
            // If we can't find an instantiator in the collection, let's create one. 
            // Even if a concurrent process already did so, then we will simply fail
            // In the worst case scenario we will end up with a few instances of the bound assembly, 
            // however it is very unlikely because at every step I will be checking on if it already exists
            try
            {
                instantiator = CreateInstantiator(bundleId);
            }
            catch (InstantiatorCreationException ie)
            {
                if (ie.IsFatal)
                    throw;

                // give it a little bit of time and retry
                Thread.Sleep(100);

                if (!Instantiators.TryGetValue(bundleId, out instantiator))
                    throw new InstantiatorException("There is a problem with this package.", null);
            }

            // this should really never happen
            if (instantiator == null)
                throw new NullReferenceException("failed to obtain instantiator");

            // now try to insert the instantiator. If it throws an exception then some other process have already done so.
            if (Instantiators.TryAdd(bundleId, instantiator))
            {
                var ret = instantiator(data);
                return ret;
            }

            return instantiator(data);
        }

        private Instantiator<T> CreateInstantiator(string bundleId)
        {
            var bundlePath = Path.Combine(Path.GetTempPath(), "HotAssembly", "Bundles", bundleId);

            try
            {
                Directory.CreateDirectory(bundlePath);
                _persistenceProvider.GetBundle(bundleId, Path.Combine(bundlePath, bundleId + ".zip"));
            }
            catch (Exception e)
            {
                throw new InstantiatorCreationException("Failed to retrieve bundle from the Persistence Provider", e, true);
            }

            try
            {
                using (var zip = ZipFile.Read(Path.Combine(bundlePath, bundleId + ".zip")))
                {
                    zip.ExtractAll(bundlePath, ExtractExistingFileAction.DoNotOverwrite);
                }
            }
            catch (Exception e)
            {
                throw new InstantiatorCreationException("Failed to unzip bundle", e, false);
            }

            var manifestPath = Path.Combine(bundlePath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new InstantiatorCreationException(string.Format("Could not find manifest at \"{0}\"", manifestPath), null, true);

            var manifest =
                JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(Path.Combine(bundlePath, "manifest.json")));

            Assembly instanceAssembly = null;
                foreach (
                    var file in
                        Directory.GetFiles(bundlePath, "*.dll", SearchOption.AllDirectories))
                {
                    var assembly = Assembly.LoadFrom(file);
                    // All referenced assemblies are in the subfolder "ReferencedAssemblies". 
                    // In the root folder must only be an assembly that contains the class to instantiate
                    if (Path.GetFileName(file) == manifest.AssemblyName)
                        instanceAssembly = assembly;
                }

            if (instanceAssembly == null)
                throw new InstantiatorCreationException(
                    string.Format("There was no assembly found on this path: \"{0}\"", bundlePath), null, true);


            var instanceRealType =
                instanceAssembly.DefinedTypes.FirstOrDefault(
                    info => info.IsClass && info.FullName == manifest.FullyQualifiedClassName);

            if (instanceRealType == null)
                throw new InstantiatorCreationException(
                    string.Format("Type \"{0}\" was not found in assembly \"{1}\"", manifest.FullyQualifiedClassName,
                        Path.Combine(bundlePath, manifest.FullyQualifiedClassName)), null, true);

            // for siimplicity I only want the initialization done through a single object.  In the future I might expand to any type of constructor
            var ctor = instanceRealType.GetConstructor(new[] {typeof (object)});

                if (ctor == null)
                    throw new InstantiatorCreationException("Constructor with a single object argument was not found", null, true);

                return GetInstantiator<T>(ctor);
        }
    }
}
