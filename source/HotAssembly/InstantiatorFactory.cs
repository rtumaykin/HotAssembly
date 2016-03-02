using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using HotAssembly.Package;
using NuGet;

namespace HotAssembly
{
    public delegate T Instantiator<T>(params object[] args);

    public class InstantiatorFactory<T> where T:class
    {
        private readonly IPackageRetriever _packageRetriever;

        static InstantiatorFactory()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.ResolveByFullAssemblyName;
        } 


        /// <summary>
        /// Dictionary to store all of the cached instantiators. All of the possible variations of constructors will be in the Value part of this dictionary.
        /// Key is the packageId, and the second Dictionary is a concatenated Types for each constructor.
        /// </summary>
        private static Dictionary<InstantiatorKey, Dictionary<string, Instantiator<T>>> _instantiators;
        protected static Dictionary<InstantiatorKey, Dictionary<string, Instantiator<T>>> Instantiators
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _instantiators,
                    () => new Dictionary<InstantiatorKey, Dictionary<string, Instantiator<T>>>());
                return _instantiators;
            }
        }

        private static ConcurrentDictionary<InstantiatorKey, object> _instantiatorLocks;
        private static ConcurrentDictionary<InstantiatorKey, object> InstantiatorLocks
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _instantiatorLocks, () => new ConcurrentDictionary<InstantiatorKey, object>());
                return _instantiatorLocks;
            }
        }

        public InstantiatorFactory(IPackageRetriever packageRetriever)
        {
            _packageRetriever = packageRetriever;
        }

        /// <summary>
        /// Creates an instance of a requested type
        /// </summary>
        /// <typeparam name="T">type of the interface to instantiate</typeparam>
        /// <param name="instantiatorKey">Full identifier of a type to instantiate including the package and version</param>
        /// <returns></returns>
        public T Instantiate(InstantiatorKey instantiatorKey)
        {
            return Instantiate(instantiatorKey, null);
        }
        public T Instantiate(InstantiatorKey instantiatorKey, object data)
        {
            return Instantiate(instantiatorKey, new[] {data});
        }
        public T Instantiate(InstantiatorKey instantiatorKey, params object [] data)
        {
            try
            {
                var instance = GetInstance(instantiatorKey, data);
                if (instance != null)
                {
                    return instance;
                }

                // OK. We did not find an instantiator. Let's try to create one. First of all let's lock an object
                var lockObject1 = new object();
                lock (lockObject1)
                {
                    if (InstantiatorLocks.TryAdd(instantiatorKey, lockObject1))
                    {
                        // if we ended up here, it means that we were first
                        Instantiators.AddRange(CreateInstantiatorsForPackage(instantiatorKey));
                    }
                    else
                    {
                        // some other process have already created (or creating) instantiator
                        // Theoretically, it is quite possible to have previous process fail, so we will need to be careful about assuming that if we got here,
                        // then we should have instantiators.
                        lock (InstantiatorLocks[instantiatorKey])
                        {
                            // try read from the instantiators first. Maybe it has already been successfully created
                            instance = GetInstance(instantiatorKey, data);
                            if (instance != null)
                            {
                                return instance;
                            }
                            Instantiators.AddRange(CreateInstantiatorsForPackage(instantiatorKey));
                        }
                    }
                    instance = GetInstance(instantiatorKey, data);
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

            throw new InstantiatorException($"Unknown error. Instantiator failed to produce an instance of {instantiatorKey}", null);
        }

        private static T GetInstance(InstantiatorKey instantiatorKey, object[] data)
        {
            if (!Instantiators.ContainsKey(instantiatorKey))
                return default(T);

            var instantiatorByType = Instantiators[instantiatorKey];

            // here it make sense to concatenate params
            var paramsHash = data == null || !data.Any() ? "" : string.Join(", ", data.Select(d => d.GetType().FullName));
            if (instantiatorByType.ContainsKey(paramsHash))
            {
                return instantiatorByType[paramsHash](data);
            }

            throw new InstantiatorException(
                $"Constructor signature {paramsHash} not found for package {instantiatorKey}", null);
        }

        private readonly string _rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotAssemblyPackages");

        /// <summary>
        /// This method creates instantiators for all of the types in the package that can be instantiated
        /// </summary>
        /// <param name="instantiatorKey"></param>
        /// <returns></returns>
        private Dictionary<InstantiatorKey, Dictionary<string, Instantiator<T>>> CreateInstantiatorsForPackage(InstantiatorKey instantiatorKey)
        {
            string packagePath;
            Directory.CreateDirectory(_rootPath);
            var returnDictionary = new Dictionary<InstantiatorKey, Dictionary<string, Instantiator<T>>>();

            try
            {
                packagePath = _packageRetriever.Retrieve(_rootPath, instantiatorKey.PackageId,
                    SemanticVersion.Parse(instantiatorKey.Version));
            }
            catch (Exception e)
            {
                throw new InstantiatorCreationException(
                    $"Package Retriever Failed to obtain the package {instantiatorKey.PackageId}.{instantiatorKey.Version}",
                    e, true);
            }

            if (string.IsNullOrWhiteSpace(packagePath))
                throw new InstantiatorCreationException(
                    $"Package Retriever Failed to obtain the package {instantiatorKey.PackageId}.{instantiatorKey.Version} from available sources",
                    null, true);

            // find the directory where the dlls are
            var libPath = Directory.GetDirectories(Path.Combine(packagePath, "lib")).FirstOrDefault() ??
                          Path.Combine(packagePath, "lib");

            var hotAssemblies = AssemblyResolver.DiscoverHotAssemblies(libPath, typeof (T));
            if (hotAssemblies != null && hotAssemblies.Any())
                AssemblyResolver.AddPackageBasePath(libPath);

            if (hotAssemblies == null)
                return returnDictionary;

            foreach (var hotType in hotAssemblies.SelectMany(hotAssembly => hotAssembly.ExportedTypes.Where(
                t =>
                    t.IsClass &&
                    typeof(T).IsAssignableFrom(t) &&
                    t.GetConstructors().Any())))
            {
                returnDictionary.Add(
                    new InstantiatorKey(instantiatorKey.PackageId, instantiatorKey.Version, hotType.FullName), 
                    hotType.GetConstructors().ToDictionary(
                    ctor =>
                        !ctor.GetParameters().Any()
                            ? ""
                            : string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.FullName)),
                    GetInstantiator));
            }

            return returnDictionary;
        }

        public static Instantiator<T> GetInstantiator(ConstructorInfo ctor)
        {
            var type = ctor.DeclaringType;
            var paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            var param =
                Expression.Parameter(typeof(object[]), "args");

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
            var compiled = lambda.Compile();
            return compiled;
        }
    }

}
