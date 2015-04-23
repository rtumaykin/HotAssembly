using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using NUnit.Framework;

namespace HotAssembly.UnitTests
{
    [TestFixture]
    public class UnitTests
    {
        [Test]
        public void Should_Successfully_Instantiate()
        {
            var fp = new FakeProvider();
            var ha = new HotAssembly.InstantiatorFactory<IComputer>(fp);
            var start = DateTime.Now;
            for (var i = 0; i < 1000000; i++)
            {
                var z = ha.Instantiate("newfile.some", "1.1", new object());
                z.Compute();
            }
            var elapsed = DateTime.Now.Subtract(start).TotalMilliseconds;
            Assert.Pass("elapsed {0} ms", elapsed);
            Debug.WriteLine("{0}", elapsed);
        }
    }

    public class FakeProvider : IPersistenceProvider
    {

        public bool GetBundle(string bundleId, string destinationPath)
        {
            foreach (var file in Directory.GetFiles(destinationPath, "*.*"))
            {
                File.Delete(file);
            }

#if DEBUG
            File.Copy(
                @"C:\Development Projects\Personal\HotAssembly\HotAssembly.Computor\bin\Debug\HotAssembly.Computor.dll",
                Path.Combine(destinationPath, string.Format("{0}.dll", bundleId)));
#else
            File.Copy(
                @"C:\Development Projects\Personal\HotAssembly\HotAssembly.Computor\bin\Release\HotAssembly.Computor.dll",
                Path.Combine(destinationPath, string.Format("{0}.dll", bundleId)));
#endif 
            {
                
            }
            using (var zip = new ZipFile())
            {
                zip.AddFile(Path.Combine(destinationPath, string.Format("{0}.dll", bundleId)), "");
                //zip.AddFile(@"C:\Development Projects\Personal\HotAssembly\HotAssembly.Computor\bin\Debug\IComputor.dll", "");
                zip.Save(Path.Combine(destinationPath, string.Format("{0}.zip", bundleId)));
                File.Delete(Path.Combine(destinationPath, string.Format("{0}.dll", bundleId)));
            }

            return true;
        }

        public bool PersistBundle(string bundleId, string sourcePath)
        {
            throw new NotImplementedException();
        }
    }
}

