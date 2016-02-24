using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotAssembly
{
    public class InstantiatorBundle<T> where T:class
    {
        public IInstantiator<T> Instantiator { get; set; }
        public object InstantiatorProxyObject { get; set; }
    }
}
