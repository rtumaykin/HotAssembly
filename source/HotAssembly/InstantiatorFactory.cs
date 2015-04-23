using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Ionic.Zip;

namespace HotAssembly
{
    public class InstantiatorFactory<T>
    {
        public delegate T Instantiator<T>(params object[] args);

        private readonly IPersistenceProvider _persistenceProvider;

        public static Instantiator<T> GetInstantiator<T>
            (ConstructorInfo ctor)
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
            var lambda = Expression.Lambda<Instantiator<T>>(Expression.Convert(newExp, typeof(T)), param);

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
        /// <param name="id">id of the assembly to instantiate.</param>
        /// <param name="version"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public T Instantiate(string id, string version, object data)
        {
            // todo: add specific exception types

            if (!Regex.IsMatch(id, @"^(@?[a-z_A-Z]\w+(?:\.@?[a-z_A-Z]\w+)*)$"))
                throw new Exception(string.Format("invalid bundle id \"{0}\"", id));

            if(!Regex.IsMatch(version, @"^\d+(?:\.\d+)+$"))
                throw new Exception(string.Format("invalid version id \"{0}\"", version));

            var bundleId = string.Format("{0}.{1}", id, version);

            Instantiator<T> instantiator;
            if (Instantiators.TryGetValue(bundleId, out instantiator)) 
                return instantiator(data);
            
            // If we can't find an instantiator in the collection, let's create one. 
            // Even if a concurrent process already did so, then we will simply fail
            // In the worst case scenario we will end up with a few instances of the bound assembly, 
            // however it is very unlikely because at every step I will be checking on if it already exists
            if (TryCreateInstantiator(bundleId, out instantiator))
            {
                if (instantiator == null)
                    throw new NullReferenceException("failed to obtain instantiator");

                // now try to insert the instantiator. If it throws an exception then some other process have already done so.
                if (Instantiators.TryAdd(bundleId, instantiator))
                {
                    var ret = instantiator(data);
                    return ret;
                }
            }
            if (!Instantiators.TryGetValue(bundleId, out instantiator))
                throw new Exception("Failed to add an instantiator, and also failed to retrieve an instantiator");

            return instantiator(data);
        }

        private bool TryCreateInstantiator(string bundleId, out Instantiator<T> instantiator)
        {
            instantiator = null;

            try
            {
                var bundlePath = string.Format("{0}\\HotAssembly\\Bundles\\{1}", Path.GetTempPath(), bundleId);
                Directory.CreateDirectory(bundlePath);

                _persistenceProvider.GetBundle(bundleId, bundlePath);

                using (var zip = ZipFile.Read(Path.Combine(bundlePath, bundleId + ".zip")))
                {
                    zip.ExtractAll(bundlePath, ExtractExistingFileAction.DoNotOverwrite);
                }

                Assembly instanceAssembly = null;
                foreach (
                    var file in
                        Directory.GetFiles(bundlePath, "*.dll", SearchOption.AllDirectories))
                {
                    var assembly = Assembly.LoadFrom(file);
                    if (file == Path.Combine(bundlePath, bundleId + ".dll"))
                        instanceAssembly = assembly;
                }

                if (instanceAssembly == null)
                    return false;

                var instanceRealType = instanceAssembly.DefinedTypes.First();
                
                if (instanceRealType == null)
                    return false;

                var ctor = instanceRealType.GetConstructors().First();
                if (ctor == null)
                    return false;

                instantiator = GetInstantiator<T>(ctor);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
