using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HotAssembly
{
    public class HotAssemblyResolver : MarshalByRefObject
    {
        public Assembly Resolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("HotAssembly"))
            {
                return typeof(Compiler).Assembly;
            }
            return null;
        }
    }
}
