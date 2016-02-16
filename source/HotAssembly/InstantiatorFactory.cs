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
using HotAssembly.Package;
using Newtonsoft.Json;
using NuGet;

namespace HotAssembly
{
    public class InstantiatorFactory<T>
    {
        private readonly IPackageRetriever _packageRetriever;

        /// <summary>
        /// Dictionary to store all of the cached instantiators. All of the possible variations of constructors will be in the Value part of this dictionary.
        /// Key is the packageId, and the second Dictionary is a concatenated Types for each constructor.
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

        public InstantiatorFactory(IPackageRetriever packageRetriever)
        {
            _packageRetriever = packageRetriever;
        }

        /// <summary>
        /// Creates an instance of a requested class
        /// </summary>
        /// <typeparam name="T">type of the interface or a base class to instantiate</typeparam>
        /// <param name="packageId">id of the assembly to instantiate.</param>
        /// <param name="version"></param>
        /// <returns></returns>
        public T Instantiate(string packageId, string version)
        {
            return Instantiate(packageId, version, null);
        }
        public T Instantiate(string packageId, string version, object data)
        {
            return Instantiate(packageId, version, new[] {data});
        }
        public T Instantiate(string packageId, string version, params object [] data)
        {
            SemanticVersion semanticVersion = null;

            try
            {
                semanticVersion = SemanticVersion.Parse(version);
            }
            catch (Exception e)
            {
                throw new InstantiatorException("Package Version is not a Semantic Version", e);
            }

            try
            {
                var versionedPackageId = $"{packageId}.{version}";

                var instance = GetInstance(versionedPackageId, data);
                if (instance != null)
                {
                    return instance;
                }

                // OK. We did not find an instantiator. Let's try to create one. First of all let's lock an object
                var lockObject1 = new object();
                lock (lockObject1)
                {
                    if (InstantiatorLocks.TryAdd(versionedPackageId, lockObject1))
                    {
                        // if we ended up here, it means that we were first
                        Instantiators.Add(versionedPackageId, CreateInstantiatorsForPackage(packageId, semanticVersion));
                    }
                    else
                    {
                        // some other process have already created (or creating) instantiator
                        // Theoretically, it is quite possible to have previous process fail, so we will need to be careful about assuming that if we got here,
                        // then we should have instantiators.
                        lock (InstantiatorLocks[versionedPackageId])
                        {
                            // try read from the instantiators first. Maybe it has already been successfully created
                            instance = GetInstance(versionedPackageId, data);
                            if (instance != null)
                            {
                                return instance;
                            }
                            Instantiators.Add(versionedPackageId, CreateInstantiatorsForPackage(packageId, semanticVersion));
                        }
                    }
                    instance = GetInstance(versionedPackageId, data);
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

            throw new InstantiatorException($"Unknown error. Instantiator failed to produce an instance of {packageId}.{version}", null);
        }

        private static T GetInstance(string versionedPackageId, object[] data)
        {


            if (Instantiators.ContainsKey(versionedPackageId))
            {
                var instantiatorByType = Instantiators[versionedPackageId];

                // here it make sense to concatenate params
                var paramsHash = data == null || !data.Any() ? "" : string.Join(", ", data.Select(d => d.GetType().FullName));
                if (instantiatorByType.ContainsKey(paramsHash))
                {
                    return instantiatorByType[paramsHash](data);
                }
                else
                {
                    throw new InstantiatorException(
                        $"Constructor signature {paramsHash} not found for package {versionedPackageId}", null);
                }
            }
            return default(T);
        }

        private readonly string _rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotAssemblyPackages");


        private Dictionary<string, Instantiator<T>> CreateInstantiatorsForPackage(string packageId, SemanticVersion semanticVersion)
        {
            var appDomainSetup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            };

            var workerAppDomain = AppDomain.CreateDomain($"{packageId}.{semanticVersion.ToNormalizedString()}", null, appDomainSetup);

            //var compiler = (InstantiatorCompiler<T>) workerAppDomain.CreateInstanceAndUnwrap (
            //    typeof (InstantiatorCompiler<T>).Assembly.GetName().Name,
            //    typeof (InstantiatorCompiler<T>).FullName,
            //    false,
            //    BindingFlags.Default, 
            //    null,
            //    new object[] { _rootPath, _packageRetriever, packageId, semanticVersion },
            //    null,
            //    null);

            var compiler = new InstantiatorCompiler<T>(_rootPath, _packageRetriever, packageId, semanticVersion);

            return compiler.CreateInstantiatorsForPackage();
        }
    }
}
