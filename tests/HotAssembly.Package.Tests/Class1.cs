using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace HotAssembly.Package.Tests
{
    [TestFixture]
    public class Class1
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

        private ConcurrentBag<string> res = new ConcurrentBag<string>();

        private void CheckRetriever()
        {
            res.Add(new NugetRetriever().Retrieve("HotAssembly"));
        }
    }
}
