using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace HotAssembly.Package.Tests
{
    [TestFixture]
    public class UnitTests
    {
        [Test]
        public void PackageGet()
        {
            var start = DateTime.Now;
            var threads = new List<Task>();
            for (var i = 0; i < 1000; i++)
            {
                threads.Add(Task.Run(() => CheckRetriever()));
            }
            Task.WaitAll(threads.ToArray());
            Assert.Pass($"Execution took {(DateTime.Now - start).TotalMilliseconds} milliseconds.");
        }

        private readonly ConcurrentBag<string> res = new ConcurrentBag<string>();

        private void CheckRetriever()
        {
            res.Add(new NugetPackageRetriever(new[]
                {@"C:\Development\Projects\HotAssembly\tests\HotAssembly.Computer.NugetPackage\bin\Debug"}).Retrieve(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotAssemblyPackages"),
                    "HotAssembly.Computer.NugetPackage"));
        }

        [Test]
        public void GetOne()
        {
            var pak =
                new NugetPackageRetriever(new[]
                {@"C:\Development\Projects\HotAssembly\tests\HotAssembly.Computer.NugetPackage\bin\Debug"}).Retrieve(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HotAssemblyPackages"),
                    "HotAssembly.Computer.NugetPackage");
        }
    }
}
