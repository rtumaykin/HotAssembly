using System;

namespace HotAssembly.Computer
{
    public class Computer : IComputer
    {
        public Computer(object data)
        {
            
        }

        public string GetAppDomain()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }
    }
}

