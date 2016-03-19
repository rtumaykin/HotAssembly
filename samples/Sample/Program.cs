using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotAssembly;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = AppDomain.CurrentDomain.BaseDirectory.Split('\\').Last(s=>!string.IsNullOrWhiteSpace(s));

            const string additorRelativePath = "..\\..\\..\\Additor.NugetPackage\\bin\\";
            const string subtractorRelativePath = "..\\..\\..\\Subtractor.NugetPackage\\bin\\";

            var nugetPackageRetriever =
                new HotAssembly.Package.NugetPackageRetriever(new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, additorRelativePath, config),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subtractorRelativePath, config)
                });

            var factory = new InstantiatorFactory<ICalculator>(nugetPackageRetriever);

            var additionResult =
                factory.Instantiate(new InstantiatorKey("Additor.NugetPackage", "1.0.0", "Sample.Additor")).Calculate(10, 5);
            Console.WriteLine($"Addition Result = {additionResult}");

            var subtractionResult =
                factory.Instantiate(new InstantiatorKey("Subtractor.NugetPackage", "1.0.0", "Sample.Subtractor")).Calculate(10, 5);
            Console.WriteLine($"Subtraction Result = {subtractionResult}");

            Console.ReadKey();
        }
    }
}
