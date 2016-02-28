using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SomeOtherProcess
{
    public class SomeThing 
    {
        public static string GetStuff()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
