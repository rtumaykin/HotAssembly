using Newtonsoft.Json;
using NUnit.Framework;

namespace HotAssembly.UnitTests
{
    [TestFixture]
    public class Misc
    {
        [Test]
        public void SerializetoJson()
        {
            var z = new manif[]
            {
                new manif {FullClassName = "abc"},
                new manif {FullClassName = "xyz"}
            };
            var cc = new[] {new manif() {FullClassName = "sasas"}};

            var xx = JsonConvert.SerializeObject(z);
            var xx0 = JsonConvert.SerializeObject(z[0]);
            var sas = JsonConvert.SerializeObject(cc);
        }


    }

    public class manif
    {
        public string FullClassName { get; set; }
    }
}
