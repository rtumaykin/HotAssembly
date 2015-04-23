using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotAssembly;
using HotAssembly.Computor;
using NUnit.Framework;

namespace BenchmarkTest
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public void LocalInvocation()
        {
            var start = DateTime.Now;
            for (int i = 0; i < 1000000; i++)
            {
                var z = (IComputer) new Computer(null);
                z.Compute();
            }
            var elapsed = DateTime.Now.Subtract(start).TotalMilliseconds;
            Assert.Pass("elapsed {0} ms", elapsed);
        }
    }
}
