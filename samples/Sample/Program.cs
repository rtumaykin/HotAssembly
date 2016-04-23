using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotAssembly;
using HotAssembly.AssemblyResolver;
using Newtonsoft.Json;
using UnsignedSharedType;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                var appDomainSetup = AppDomain.CurrentDomain.SetupInformation;

                appDomainSetup.DisallowApplicationBaseProbing = true;
                var workerDomain = AppDomain.CreateDomain("Worker Domain", null, appDomainSetup);
                workerDomain.ExecuteAssembly(typeof (Program).Assembly.Location);
            }
            else
            {
                DefaultContext.WireUpResolver();

                var config = AppDomain.CurrentDomain.BaseDirectory.Split('\\').Last(s => !string.IsNullOrWhiteSpace(s));

                const string additorRelativePath = "..\\..\\..\\Additor.NugetPackage\\bin\\";
                const string subtractorRelativePath = "..\\..\\..\\Subtractor.NugetPackage\\bin\\";

                var nugetPackageRetriever =
                    new HotAssembly.Package.NugetPackageRetriever(new[]
                    {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, additorRelativePath, config),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subtractorRelativePath, config)
                    });

                // cleanup
                var additorFolderPath =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "HotAssemblyPackages", "Additor.NugetPackage.1.0.0");

                var subtractorFolderPath =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "HotAssemblyPackages", "Subtractor.NugetPackage.1.0.0");

                if (Directory.Exists(additorFolderPath))
                    Directory.Delete(additorFolderPath, true);

                if (Directory.Exists(subtractorFolderPath))
                    Directory.Delete(subtractorFolderPath, true);

                var factory = new InstantiatorFactory<ICalculator>(nugetPackageRetriever);
                Console.WriteLine(
                    $"Main Program SharedType Runtime Version: {typeof (SomeType).Assembly.ImageRuntimeVersion}. Codebase: {typeof (SomeType).Assembly.CodeBase}.");

                var additionResult =
                    factory.Instantiate(new InstantiatorKey("Additor.NugetPackage", "1.0.0", "Sample.Additor"))
                        .Calculate(10, 5);
                Console.WriteLine($"Addition Result = {additionResult}");

                var subtractionResult =
                    factory.Instantiate(new InstantiatorKey("Subtractor.NugetPackage", "1.0.0", "Sample.Subtractor"))
                        .Calculate(10, 5);
                Console.WriteLine($"Subtraction Result = {subtractionResult}");

                Console.ReadKey();
            }
        }
    }
}
