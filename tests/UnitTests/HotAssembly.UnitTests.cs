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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HotAssembly.Package;
using Xunit;
using Xunit.Abstractions;

namespace HotAssembly.Tests
{
    public class UnitTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _basePath;
        private const string _nugetPackageLocation = @"..\..\..\TestObjects\HotAssembly.Computer.NugetPackage\bin";

        public UnitTests(ITestOutputHelper output)
        {
            _output = output;

            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                $"HotAssemblyPackages_Test_{Guid.NewGuid().ToString("N")}");

            Directory.CreateDirectory(_basePath);
        }

        [Fact]
        public void Should_Successfully_Instantiate()
        {
            var configName = AppDomain.CurrentDomain.BaseDirectory.Split('\\').Last();
            var fp = new NugetPackageRetriever(new [] {Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        _nugetPackageLocation, configName)});
            var ha = new InstantiatorFactory<IComputer>(fp);
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
            _output.WriteLine($"Total elapsed {elapsed} ms.");
        }

        [Fact]
        public void Should_Successfully_Instantiate_Multithreaded()
        {
            var configName = AppDomain.CurrentDomain.BaseDirectory.Split('\\').Last();
            var fp = new NugetPackageRetriever(new[] {Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        _nugetPackageLocation, configName)});

            var ha = new InstantiatorFactory<IComputer>(fp);
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
            _output.WriteLine($"Total elapsed {elapsed} ms.");
        }

        [Fact]
        // Before running this test compile HotAssembly.Computer project!!!
        public void Should_Fail_No_Ctor()
        {
            var configName = AppDomain.CurrentDomain.BaseDirectory.Split('\\').Last();
            var fp = new NugetPackageRetriever(new[] {Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    _nugetPackageLocation, configName)});

            var ha = new InstantiatorFactory<IComputer>(fp);
            Exception ex = Assert.Throws<InstantiatorException>(() =>
            {
                var z =
                    ha.Instantiate(
                        new InstantiatorKey("HotAssembly.Computer.NugetPackage", "1.0.0",
                            "HotAssembly.Computer.Computer"), 100);
            }
        );

            Assert.Equal(ex.GetType(), typeof(InstantiatorException));
        }


        [Fact]
        // Before running this test compile HotAssembly.Computer project!!!
        public void Should_Pass_One()
        {
            var configName = AppDomain.CurrentDomain.BaseDirectory.Split('\\').Last();
            var fp = new NugetPackageRetriever(new[] {Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    _nugetPackageLocation, configName)});
            var ha = new InstantiatorFactory<IComputer>(fp);
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

