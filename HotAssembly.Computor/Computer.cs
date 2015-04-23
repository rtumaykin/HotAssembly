using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotAssembly.Computor
{
    public class Computer : IComputer
    {
        public Computer(object data)
        {
            
        }

        public int Compute()
        {
            return new Random().Next(1, 100);
        }
    }
}

