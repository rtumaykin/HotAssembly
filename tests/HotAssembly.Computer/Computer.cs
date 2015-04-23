using System;

namespace HotAssembly.Computer
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

