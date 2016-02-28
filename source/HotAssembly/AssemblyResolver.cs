using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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


        public static Assembly ResolveByClassName(string basePath, string classFullName)
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
                    files.FirstOrDefault(p => instanceInNewDomain.DoesAssemblyContainClass(p, classFullName));

                return !string.IsNullOrWhiteSpace(assemblyPath) ? Assembly.LoadFile(assemblyPath) : null;
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
        internal bool DoesAssemblyContainClass
            (string filePath, string classFullName)
        {
            try
            {
                var assembly = Assembly.LoadFrom(filePath);
                return assembly.ExportedTypes.Any(t => t.FullName == classFullName);
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
