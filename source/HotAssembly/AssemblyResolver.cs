using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotAssembly.Package;

namespace HotAssembly
{
    public class AssemblyResolver
    {
        private static readonly ConcurrentDictionary<string, bool> InstalledPackagesBasePaths = new ConcurrentDictionary<string, bool>();

        public static void AddPackageBasePath(string basePath)
        {
            InstalledPackagesBasePaths.TryAdd(basePath, false);
        } 

        public static Assembly ResolveByFullAssemblyName(object sender, ResolveEventArgs args)
        {
            var basePath = Path.GetDirectoryName(args.RequestingAssembly.Location);
            var assemblyFullName = args.Name;

            if (string.IsNullOrWhiteSpace(basePath))
                return null;

            var dummyOutValue = false;

            // only resolve the paths that have been added by HotAssembly
            if (!InstalledPackagesBasePaths.TryGetValue(basePath, out dummyOutValue))
                return null;

            AppDomain newDomain = null;

            try
            {
                var newDomainSetup = new AppDomainSetup()
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
                };

                newDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString("N"), null, newDomainSetup);
                var instanceInNewDomain = (ResolverAppDomainAgent)newDomain.CreateInstanceFromAndUnwrap(
                    typeof(ResolverAppDomainAgent).Assembly.Location,
                    typeof(ResolverAppDomainAgent).FullName,
                    true,
                    BindingFlags.Default,
                    null,
                    null,
                    null,
                    null);

                var files = Directory.GetFiles(basePath, "*.*");
                var assemblyPath =
                    files.FirstOrDefault(p => instanceInNewDomain.CompareAssemblyFullName(p, assemblyFullName));

                return !string.IsNullOrWhiteSpace(assemblyPath) ? Assembly.LoadFile(assemblyPath) : null;
            }
            finally
            {
                if (newDomain != null)
                    AppDomain.Unload(newDomain);
            }
        }

        public static Assembly[] DiscoverHotAssemblies(string basePath, PackageManifest[] manifests, Type interfaceToLookFor)
        {
            AppDomain newDomain = null;
            try
            {
                var newDomainSetup = new AppDomainSetup()
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory
                };

                newDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString("N"), null, newDomainSetup);
                var instanceInNewDomain = (ResolverAppDomainAgent) newDomain.CreateInstanceFromAndUnwrap(
                    typeof (ResolverAppDomainAgent).Assembly.Location,
                    typeof (ResolverAppDomainAgent).FullName,
                    true,
                    BindingFlags.Default,
                    null,
                    null,
                    null,
                    null);

                var files = Directory.GetFiles(basePath, "*.*");
                return files.Where(
                    p =>
                        instanceInNewDomain.DoesAssemblyContainClassOrHotAssemblyAttributeOfRequestedType(p,
                            manifests, interfaceToLookFor))
                    .Select(assemblyPath => Assembly.LoadFile(assemblyPath))
                    .ToArray();
            }
            finally
            {
                if (newDomain != null)
                    AppDomain.Unload(newDomain);
            }
        }
    }

    internal class ResolverAppDomainAgent : MarshalByRefObject
    {
        internal bool DoesAssemblyContainClassOrHotAssemblyAttributeOfRequestedType
            (string filePath, PackageManifest[] manifests, Type inerfaceTypeToSearchFor)
        {
            try
            {
                var assembly = Assembly.LoadFrom(filePath);
                return
                    assembly.ExportedTypes.Any(
                        t =>
                            // if the line below does not work, we can compare by the full name
                            t.GetInterfaces().Any(i => i == inerfaceTypeToSearchFor) &&
                            t.GetConstructors().Any() &&
                            (manifests.Any(m => m.ClassFullName == t.FullName) ||
                            t.GetCustomAttributes(typeof (HotAssemblyAttribute), true).Any()));
            }
            catch
            {
                return false;
            }
        }
        internal bool CompareAssemblyFullName(string filePath, string assemblyFullName)
        {
            try
            {
                var assembly = Assembly.LoadFrom(filePath);
                return assembly.FullName == assemblyFullName;
            }
            catch
            {
                return false;
            }

        }
    }
}
