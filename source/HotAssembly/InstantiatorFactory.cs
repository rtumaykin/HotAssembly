using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
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

        /// <summary>
        /// Dictionary to store all of the cached instantiators. All of the possible variations of constructors will be in the Value part of this dictionary.
        /// Key is the bundleId, and the second Dictionary is a concatenated Types for each constructor.
        /// </summary>
        private static Dictionary<string, Dictionary<string, Instantiator<T>>> _instantiators;
        private static Dictionary<string, Dictionary<string, Instantiator<T>>> Instantiators
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _instantiators,
                    () => new Dictionary<string, Dictionary<string, Instantiator<T>>>());
                return _instantiators;
            }
        }

        private static ConcurrentDictionary<string, object> _instantiatorLocks;
        private static ConcurrentDictionary<string, object> InstantiatorLocks
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _instantiatorLocks, () => new ConcurrentDictionary<string, object>());
                return _instantiatorLocks;
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

        public T Instantiate(string bundleId)
        {
            return Instantiate(bundleId, null);
        }

        public T Instantiate(string bundleId, object data)
        {
            return Instantiate(bundleId, new[] {data});
        }
        public T Instantiate(string bundleId, params object [] data)
        {
            if (Regex.IsMatch(bundleId, "[^a-zA-Z0-9_.-]"))
                throw new InstantiatorException("Buldle Id should only contain letters, digits, dashes, underscores and dots.", null);

            try
            {
                var instance = GetInstance(bundleId, data);
                if (instance != null)
                {
                    return instance;
                }

                // OK. We did noit find an instantiator. Let's try to create one. First of all let's lock an object
                var lockObject1 = new object();
                lock (lockObject1)
                {
                    if (InstantiatorLocks.TryAdd(bundleId, lockObject1))
                    {
                        // if we ended up here, it means that we were first
                        Instantiators.Add(bundleId, CreateInstantiatorsForBundle(bundleId));
                    }
                    else
                    {
                        // some other process have already created (or creating) instantiator
                        // Theoretically, it is quite possible to have previous process fail, so we will need to be careful about assuming that if we got here,
                        // then we should have instantiators.
                        lock (InstantiatorLocks[bundleId])
                        {
                            // try read from the instantiators first. Maybe it has already been successfully created
                            instance = GetInstance(bundleId, data);
                            if (instance != null)
                            {
                                return instance;
                            }
                            Instantiators.Add(bundleId, CreateInstantiatorsForBundle(bundleId));
                        }
                    }
                    instance = GetInstance(bundleId, data);
                    if (instance != null)
                    {
                        return instance;
                    }
                }
            }
            catch (Exception e)
            {
                throw new InstantiatorException("Error occurred during instantiation", e);
            }

            throw new InstantiatorException($"Unknown error. Instantiator failed to produce an instance of {bundleId}", null);
        }

        private static T GetInstance(string bundleId, object[] data)
        {
            if (Instantiators.ContainsKey(bundleId))
            {
                var instantiatorByType = Instantiators[bundleId];

                // here it make sense to concatenate params
                var paramsHash = data == null || !data.Any() ? "" : string.Join(", ", data.Select(d => d.GetType().FullName));
                if (instantiatorByType.ContainsKey(paramsHash))
                {
                    return instantiatorByType[paramsHash](data);
                }
                else
                {
                    throw new InstantiatorException(
                        $"Constructor signature {paramsHash} not found for bundle {bundleId}", null);
                }
            }
            return default(T);
        }

        private Dictionary<string, Instantiator<T>> CreateInstantiatorsForBundle(string bundleId)
        {
            // I don't want to overwrite or conflict over the path. So I am randomizing everything
            var bundlePath = Path.Combine(Path.GetTempPath(), "HotAssembly", "Bundles", bundleId,
                Guid.NewGuid().ToString("N"));
            var tempZipFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(bundlePath);
                _persistenceProvider.GetBundle(bundleId, tempZipFileName);
            }
            catch (Exception e)
            {
                throw new InstantiatorCreationException("Failed to retrieve bundle from the Persistence Provider", e, true);
            }

            try
            {
                ZipFile.ExtractToDirectory(tempZipFileName, bundlePath);
            }
            catch (Exception e)
            {
                throw new InstantiatorCreationException("Failed to unzip bundle", e, false);
            }

            var manifestPath = Path.Combine(bundlePath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new InstantiatorCreationException($"Could not find manifest at \"{manifestPath}\"", null, true);

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
                    $"There was no assembly found on this path: \"{bundlePath}\"", null, true);


            var instanceRealType =
                instanceAssembly.DefinedTypes.FirstOrDefault(
                    info => info.IsClass && info.FullName == manifest.FullyQualifiedClassName);

            if (instanceRealType == null)
                throw new InstantiatorCreationException(
                    $"Type \"{manifest.FullyQualifiedClassName}\" was not found in assembly \"{Path.Combine(bundlePath, manifest.FullyQualifiedClassName)}\"", null, true);

            // for siimplicity I only want the initialization done through a single object.  In the future I might expand to any type of constructor
            var ctors = instanceRealType.GetConstructors();

            if (ctors == null || !ctors.Any())
                throw new InstantiatorCreationException(
                    $"No public constructors for type {instanceRealType} were found", null, true);

            return
                ctors.ToDictionary(
                ctor => !ctor.GetParameters().Any() ? "" : string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.FullName)),
                    ctor => GetInstantiator<T>(ctor));
        }
    }
}
