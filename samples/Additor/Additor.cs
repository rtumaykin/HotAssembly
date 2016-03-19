using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample 
{
    public class Additor : ICalculator
    {
        public int Calculate(int a, int b)
        {
            return a + b;
        }
    }
}
