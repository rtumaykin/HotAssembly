using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
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
            var fp = new FakeProvider();
            var ha = new HotAssembly.InstantiatorFactory<IComputer>(fp);
            {
                // let it jit compile
                var z = ha.Instantiate("newfile.some.1.1", new object());
            }
            
            var start = DateTime.Now;
            for (var i = 0; i < 1000000; i++)
            {
                var z = ha.Instantiate("newfile.some.1.1", new object());
                z.Compute();
            }
            var elapsed = DateTime.Now.Subtract(start).TotalMilliseconds;
            Assert.Pass("Total elapsed {0} ms.", elapsed);
            Debug.WriteLine("{0}", elapsed);
        }
    }

    public class FakeProvider : IPersistenceProvider
    {

        public void GetBundle(string bundleId, string destinationPath)
        {
            foreach (var file in Directory.GetFiles(destinationPath, "*.*"))
            {
                File.Delete(file);
            }

            var config = 
#if DEBUG
                "Debug" 
#else 
                "Release" 
#endif
                ;

            File.Copy(
                new Uri(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase), "../../../HotAssembly.Computer/bin", config, "HotAssembly.Computer.dll")).LocalPath,
                Path.Combine(destinationPath, string.Format("{0}.test.dll", bundleId)));

            var manifest = new
            {
                FullyQualifiedClassName = "HotAssembly.Computer.Computer",
                AssemblyName = string.Format("{0}.test.dll", bundleId)
            };

            File.WriteAllText(Path.Combine(destinationPath, "manifest.json"), JsonConvert.SerializeObject(manifest));

            using (var zip = new ZipFile())
            {
                zip.AddFile(Path.Combine(destinationPath, string.Format("{0}.test.dll", bundleId)), "");
                zip.AddFile(Path.Combine(destinationPath, "manifest.json"), "");
                zip.Save(Path.Combine(destinationPath, string.Format("{0}.zip", bundleId)));
                File.Delete(Path.Combine(destinationPath, string.Format("{0}.test.dll", bundleId)));
                File.Delete(Path.Combine(destinationPath, "manifest.json"));
            }
        }

        public void PersistBundle(string bundleId, string sourcePath)
        {
            throw new NotImplementedException();
        }
    }
}

