using System;
using System.IO;
using System.Threading;
using NuGet;

namespace HotAssembly.Package
{
    /// <summary>
    /// NugetRetriever retrieves and stores locally a requested nuget package from a nuget repository
    /// </summary>
    public class NugetRetriever : IRetriever
    {
        private readonly string[] _repositories;

        /// <summary>
        /// This constructor reads HotAssembly/NuGetRepos section from the application 
        /// configuration file to initialize the repositories. If the section does not 
        /// exist or not configured, it initialized with default NuGet Url
        /// "https://packages.nuget.org/api/v2"
        /// </summary>
        public NugetRetriever()
        {
            _repositories = new[] {"https://packages.nuget.org/api/v2"};
        }
        /// <summary>
        /// This constructor initializes the class using a collection of Uri paths to the repositories
        /// </summary>
        /// <param name="repositories"></param>
        public NugetRetriever(string[] repositories)
        {
            _repositories = repositories;
        }

        public string Retrieve(string packageName, SemanticVersion version)
        {
            var basePath = Path.Combine(Path.GetTempPath(), "HotAssemblyRepositories");
            Directory.CreateDirectory(basePath);
            foreach (var repositoryUri in _repositories)
            {
                var repo = PackageRepositoryFactory.Default.CreateRepository(repositoryUri);
                var package = version == null ? repo.FindPackage(packageName) : repo.FindPackage(packageName, version);

                if (package != null)
                {
                    var packageDestinationFolder = Path.Combine(basePath, $"{package.Id}.{package.Version}");

                    var now = DateTime.Now;
                    // ultimately either this or another process will end up creating this directory
                    while (!Directory.Exists(packageDestinationFolder) && (DateTime.Now - now).TotalSeconds < 30)
                    {
                        var lockFileName = $"{packageDestinationFolder}.lock";
                        // if file does not exist then we can create it and lock 
                        if (!File.Exists(lockFileName))
                        {
                            try
                            {
                                // use this to lock the 
                                using (File.Create(lockFileName, 1024, FileOptions.DeleteOnClose))
                                {
                                    if (Directory.Exists(packageDestinationFolder))
                                        return packageDestinationFolder;
                                    try
                                    {
                                        // by now other process might have created this folder and unpacked the package
                                        package.ExtractContents(new PhysicalFileSystem(basePath),
                                            packageDestinationFolder);

                                        return packageDestinationFolder;
                                    }
                                    catch (Exception)
                                    {
                                        // Cleanup
                                        if (Directory.Exists(packageDestinationFolder))
                                            Directory.Delete(packageDestinationFolder, true);
                                        // continue --> Directory has not been created so we will run another loop
                                    }
                                }

                            }
                            catch (Exception)
                            {
                                // suppress the error. All we need to know is that we can't create lock file
                            }
                        }
                        else
                        {
                            Thread.Sleep(50);
                        }
                    }
                    return Directory.Exists(packageDestinationFolder) ? packageDestinationFolder : null;
                }
            }
            // package not found
            return null;
        }

        public string Retrieve(string packageName)
        {
            return Retrieve(packageName, null);
        }
    }
}
