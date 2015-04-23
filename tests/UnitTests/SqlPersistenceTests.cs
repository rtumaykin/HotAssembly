using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace HotAssembly.UnitTests
{
    [TestFixture]
    public class SqlPersistenceTests
    {
        [Test]
        public void Should_Persist_And_Get_Data()
        {
            var sqlpl = new SqlPersistenceProvider();
            var uniq = Guid.NewGuid();
            var srcPath = Path.Combine(Path.GetTempPath(), string.Format("{0:N}.txt", uniq));
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "dest"));
            var destPath = Path.Combine(Path.GetTempPath(), "dest", string.Format("{0:N}.txt", uniq));
            System.IO.File.WriteAllText(srcPath, uniq.ToString("N"));
            sqlpl.PersistBundle(uniq.ToString("N"), srcPath);
            sqlpl.GetBundle(uniq.ToString("N"), destPath);
            Assert.IsTrue(File.Exists(destPath));
        }
    }
}
