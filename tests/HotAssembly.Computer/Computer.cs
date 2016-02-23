using System;

namespace HotAssembly.Computer
{
    [Serializable]
    public class Computer : IComputer
    {
        public Computer()
        {
            
        }

        public string GetAppDomain()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }
    }
}

