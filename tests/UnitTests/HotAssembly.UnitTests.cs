using System;
using System.Collections.Generic;
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
            var z = ha.Instantiate("newfile.some", "1.1", new object());

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


            File.Copy(
                @"C:\Development Projects\Personal\HotAssembly\HotAssembly.Computor\bin\Debug\HotAssembly.Computor.dll",
                Path.Combine(destinationPath, string.Format("{0}.dll", bundleId)));

            using (var zip = new ZipFile())
            {
                zip.AddFile(Path.Combine(destinationPath, string.Format("{0}.dll", bundleId)), "");
                zip.AddFile(
                    @"C:\Development Projects\Personal\HotAssembly\HotAssembly.Computor\bin\Debug\IComputor.dll", "");
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

