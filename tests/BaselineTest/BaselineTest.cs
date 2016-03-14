using System;
using HotAssembly;
using HotAssembly.Computer;
using NUnit.Framework;

namespace BaselineTest
{
    [TestFixture]
    public class BaselineTest
    {
        [Test]
        public void LocalInvocation()
        {
            var start = DateTime.Now;
            for (int i = 0; i < 1000000; i++)
            {
                var z = (IComputer) new Computer();
                var x = z.GetAppDomain();
            }
            var elapsed = DateTime.Now.Subtract(start).TotalMilliseconds;
            Assert.Pass("elapsed {0} ms", elapsed);
        }
    }
}
