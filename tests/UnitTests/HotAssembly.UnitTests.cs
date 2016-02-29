using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotAssembly.Package;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HotAssembly.UnitTests
{
    [TestFixture]
    public class UnitTests
    {
        [Test]
        // Before running this test compile HotAssembly.Computer project!!!
        public void Should_Successfully_Instantiate()
        {
            var fp = new NugetPackageRetriever(new [] {@"C:\Development\Projects\HotAssembly\tests\HotAssembly.Computer.NugetPackage\bin\Debug"});
            var ha = new HotAssembly.InstantiatorFactory<IComputer>(fp);
            {
                // let it jit compile
                var z = ha.Instantiate(new InstantiatorKey("HotAssembly.Computer.NugetPackage", "1.0.0", "HotAssembly.Computer.Computer"));
            }
            
            var start = DateTime.Now;
            for (var i = 0; i < 1000000; i++)
            {
                var z = ha.Instantiate(new InstantiatorKey("HotAssembly.Computer.NugetPackage", "1.0.0", "HotAssembly.Computer.Computer"));
                var x = z.GetAppDomain();
            }
            var elapsed = DateTime.Now.Subtract(start).TotalMilliseconds;
            Assert.Pass("Total elapsed {0} ms.", elapsed);
            Debug.WriteLine("{0}", elapsed);
        }

        [Test]
        // Before running this test compile HotAssembly.Computer project!!!
        public void Should_Successfully_Instantiate_Multithreaded()
        {
            var fp = new NugetPackageRetriever(new[] { @"C:\Development\Projects\HotAssembly\tests\HotAssembly.Computer.NugetPackage\bin\Debug" });
            var ha = new HotAssembly.InstantiatorFactory<IComputer>(fp);
            var tasks = new List<Task>();

            var start = DateTime.Now;
            for (var i = 0; i < 1000000; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var z = ha.Instantiate(new InstantiatorKey("HotAssembly.Computer.NugetPackage", "1.0.0", "HotAssembly.Computer.Computer"));
                    var x = z.GetAppDomain();
                }));
            }
            Task.WaitAll(tasks.ToArray());

            var elapsed = DateTime.Now.Subtract(start).TotalMilliseconds;
            Assert.Pass("Total elapsed {0} ms.", elapsed);
            Debug.WriteLine("{0}", elapsed);
        }

        [Test]
        // Before running this test compile HotAssembly.Computer project!!!
        public void Should_Fail_No_Ctor()
        {
            Exception e = null;
            try
            {
                var fp = new NugetPackageRetriever(new[] { @"C:\Development\Projects\HotAssembly\tests\HotAssembly.Computer.NugetPackage\bin\Debug" });
                var ha = new HotAssembly.InstantiatorFactory<IComputer>(fp);
                var z = ha.Instantiate(new InstantiatorKey("HotAssembly.Computer.NugetPackage", "1.0.0", "HotAssembly.Computer.Computer"), 100);
                var x = z.GetAppDomain();
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.IsNotNull(e);
        }


        [Test]
        // Before running this test compile HotAssembly.Computer project!!!
        public void Should_Pass_One()
        {
            var fp =
                new NugetPackageRetriever(new[]
                {@"C:\Development\Projects\HotAssembly\tests\HotAssembly.Computer.NugetPackage\bin\Debug"});
            var ha = new HotAssembly.InstantiatorFactory<IComputer>(fp);
            var z =
                ha.Instantiate(new InstantiatorKey("HotAssembly.Computer.NugetPackage", "1.0.0",
                    "HotAssembly.Computer.Computer"));
            var x = z.GetAppDomain();
            var z1 =
                ha.Instantiate(new InstantiatorKey("HotAssembly.Computer.NugetPackage", "1.0.0",
                    "HotAssembly.Computer.Computer1"));
            var x1 = z1.GetAppDomain();
        }
    }
}

