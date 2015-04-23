using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Ionic.Zip;

namespace HotAssembly
{
    public class InstantiatorFactory
    {
        private readonly IPersistenceProvider _persistenceProvider;

        private static ConcurrentDictionary<string, IInstantiator> _instantiators;
        private static ConcurrentDictionary<string, IInstantiator> Instantiators
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _instantiators,
                    () => new ConcurrentDictionary<string, IInstantiator>());
                return _instantiators;
            }
        }

        private string GenerateInstantiatorCode(Type instanceType, string sourcePath)
        {
            var appDomainSetup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            };

            var workerAppDomain = AppDomain.CreateDomain("InstantiatorCodeBuilder", null, appDomainSetup);

            try
            {
                var instantiatorCodeBuilder = (InstantiatorCodeBuilder) workerAppDomain.CreateInstanceAndUnwrap(
                    typeof (InstantiatorCodeBuilder).Assembly.GetName().Name,
                    typeof (InstantiatorCodeBuilder).FullName);


                var codeBuilderResults = instantiatorCodeBuilder.GetInstantiatorCode(instanceType, sourcePath);
                return codeBuilderResults;
            }
            finally
            {
                AppDomain.Unload(workerAppDomain);
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
        public T Instantiate<T>(string id, string version, object data)
        {
            // todo: add specific exception types

            if (!Regex.IsMatch(id, @"^using (@?[a-z_A-Z]\w+(?:\.@?[a-z_A-Z]\w+)*);$"))
                throw new Exception(string.Format("invalid bundle id \"{0}\"", id));

            if(!Regex.IsMatch(version, @"^\d+(?:\.\d+)+$"))
                throw new Exception(string.Format("invalid version id \"{0}\"", version));

            var bundleId = string.Format("{0}.{1}", id, version);

            IInstantiator instantiator;
            if (Instantiators.TryGetValue(bundleId, out instantiator)) 
                return instantiator.Instantiate<T>(data);
            
            // If we can't find an instantiator in the collection, let's create one. 
            // Even if a concurrent process already did so, then we will simply fail
            // In the worst case scenario we will end up with a few instances of the bound assembly, 
            // however it is very unlikely because at every step I will be checking on if it already exists
            if (TryCreateInstantiator(typeof (T), bundleId, out instantiator))
            {
                if (instantiator == null)
                    throw new NullReferenceException("failed to obtain instantiator");

                // now try to insert the instantiator. If it throws an exception then some other process have already done so.
                if (Instantiators.TryAdd(bundleId, instantiator))
                    return instantiator.Instantiate<T>(data);
            }
            if (!Instantiators.TryGetValue(bundleId, out instantiator))
                throw new Exception("Failed to add an instantiator, and also failed to retrieve an instantiator");

            return instantiator.Instantiate<T>(data);
        }

        private bool TryCreateInstantiator(Type instanceType, string bundleId, out IInstantiator instantiator)
        {
            instantiator = null;

            try
            {
                var bundlePath = string.Format("{0}\\HotAssembly\\Bundles\\{1}", Path.GetTempPath(), bundleId);
                Directory.CreateDirectory(bundlePath);

                _persistenceProvider.GetBundle(bundleId, bundlePath);

                using (var zip = ZipFile.Read(Path.Combine(bundlePath, bundleId + ".zip")))
                {
                    zip.ExtractAll(bundlePath, ExtractExistingFileAction.Throw);
                }

                var code = GenerateInstantiatorCode(instanceType, Path.Combine(bundlePath, bundleId + ".dll"));
                var referencedAssemblies = new List<string>
                {
                    instanceType.Assembly.Location,
                    typeof (IInstantiator).Assembly.Location,
                    "System.Core.dll",
                    "mscorlib.dll",
                    "System.dll"
                };

                referencedAssemblies.AddRange(Directory.GetFiles(bundlePath, "*.dll", SearchOption.AllDirectories));

                var compilerResults = CompileInstantiator(Path.Combine(bundlePath, bundleId + ".dll"), code, bundlePath,
                    referencedAssemblies.GroupBy(s => s).Select(x => x.First()).ToArray());

                if (compilerResults.Errors.Count > 0)
                    throw new Exception("failed to compile");

                foreach (
                    var file in
                        Directory.GetFiles(bundlePath, "*.dll", SearchOption.AllDirectories)
                            .Where(file => file != compilerResults.PathToAssembly))
                {
                    Assembly.LoadFrom(file);
                }
                var instantiatorAssembly = Assembly.LoadFrom(compilerResults.PathToAssembly);

                Type instantiatorType = instantiatorAssembly.DefinedTypes.First();

                if (instantiatorType == null)

                    return false;
                var instantiatorConstructorInfo = instantiatorType.GetConstructor(new Type[] {});
                if (instantiatorConstructorInfo != null)
                {
                    instantiator = instantiatorConstructorInfo.Invoke(new object[] {}) as IInstantiator;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected CompilerResults CompileInstantiator(string bundlePath, string code, string outputPath, string[] referencedAssemblies)
        {
            var appDomainSetup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            };

            var instantiatorFileName = Path.Combine(outputPath, string.Format("{0}.dll", Guid.NewGuid()));
            var workerAppDomain = AppDomain.CreateDomain("Compiler", null, appDomainSetup);

            try
            {
                var compiler = (Compiler)workerAppDomain.CreateInstanceAndUnwrap(
                  typeof(Compiler).Assembly.GetName().Name,
                  typeof(Compiler).FullName);


                var compilerResults = compiler.Compile(code, false, instantiatorFileName, referencedAssemblies);
                return compilerResults;
            }
            finally
            {
                AppDomain.Unload(workerAppDomain);
            }
        }
    }
}
