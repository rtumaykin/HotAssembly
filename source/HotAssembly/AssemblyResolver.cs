//-----------------------------------------------------------------------
//Copyright 2015-2016 Roman Tumaykin
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//-----------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HotAssembly
{
    /// <summary>
    /// Helper class to perform actions related to Assembly reference resolution
    /// </summary>
    public class AssemblyResolver
    {
        /// <summary>
        /// Collection of the base paths to the packages. Used to limit searches only within these paths and subfolders
        /// </summary>
        private static readonly ConcurrentDictionary<string, bool> InstalledPackagesRootPaths =
            new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Adds a new path to the base packages paths (<see cref="InstalledPackagesRootPaths"/>). If a path already exists, does nothing.
        /// </summary>
        /// <param name="rootPath"></param>
        public static void AddPackageRootPath(string rootPath)
        {
            InstalledPackagesRootPaths.TryAdd(rootPath, false);
        }

        /// <summary>
        /// Event Handler which resolves the assembly full name to an assembly, using the calling assembly path as a 
        /// search folder. Search is only performed if the caller path is one of the paths contained in 
        /// <see cref="InstalledPackagesRootPaths"/>.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="args">Resolve Parameters</param>
        /// <returns></returns>
        public static Assembly ResolveByFullAssemblyName(object sender, ResolveEventArgs args)
        {
            var basePath = Path.GetDirectoryName(args.RequestingAssembly.Location);
            var assemblyFullName = args.Name;

            return ResolveByFullAssemblyNameInternal(basePath, assemblyFullName);
        }

        /// <summary>
        /// Event Handler which resolves the assembly full name to an assembly, using the calling assembly path as a 
        /// search folder. Search is only performed if the caller path is one of the paths contained in 
        /// </summary>
        /// /// <see cref="InstalledPackagesRootPaths"/>.
        /// <param name="basePath">Base Path where an assembly with a given Full Name is searched in</param>
        /// <param name="assemblyFullName">Assembly Full Name</param>
        /// <returns></returns>
        internal static Assembly ResolveByFullAssemblyNameInternal(string basePath, string assemblyFullName)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                return null;

            bool dummyOutValue;

            // only resolve the paths that have been added by HotAssembly
            if (!InstalledPackagesRootPaths.TryGetValue(basePath, out dummyOutValue))
                return null;

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

        /// <summary>
        /// Discovers all assemblies, located in <see cref="basePath"/> and contain types derived from <see cref="baseType"/>.
        /// </summary>
        /// <param name="basePath">Path to search in, including subfolders</param>
        /// <param name="baseType">Base Type of the types we are searching for.</param>
        /// <returns></returns>
        public static Assembly[] DiscoverHotAssemblies(string basePath, Type baseType)
        {

            var files = Directory.GetFiles(basePath, "*.*");
            return files.Where(
                p =>
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

                        return instanceInNewDomain.DoesAssemblyContainInheritedTypes(p,
                            baseType);
                    }
                    finally
                    {
                        if (newDomain != null)
                            AppDomain.Unload(newDomain);
                    }
                })
                .Select(Assembly.LoadFile)
                .ToArray();
        }
    }

    /// <summary>
    /// Agent type that is being injected into a new AppDomain to perform actual search tasks
    /// </summary>
    internal class ResolverAppDomainAgent : MarshalByRefObject
    {
        /// <summary>
        /// Searches in an assembly file (<see cref="assemblyPath"/>) for implementations of <see cref="baseType"/>.
        /// </summary>
        /// <param name="assemblyPath">Path to Assembly</param>
        /// <param name="baseType">Type to search for implementations of.</param>
        /// <returns></returns>
        internal bool DoesAssemblyContainInheritedTypes
            (string assemblyPath, Type baseType)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                return
                    assembly.ExportedTypes.Any(
                        t =>
                            t.IsClass &&
                            baseType.IsAssignableFrom(t) &&
                            t.GetConstructors().Any());
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Retrieves an assembly full name and compares to a provided <see cref="assemblyFullName"/>.
        /// </summary>
        /// <param name="assemblyPath">Assembly File Path</param>
        /// <param name="assemblyFullName">Full Name to compare to</param>
        /// <returns></returns>
        internal bool CompareAssemblyFullName(string assemblyPath, string assemblyFullName)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                return assembly.FullName == assemblyFullName;
            }
            catch
            {
                return false;
            }

        }
    }
}
