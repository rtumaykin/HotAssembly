using System;
using System.IO;
using Newtonsoft.Json;

namespace HotAssembly.Computer
{
    [Serializable]
    public class Computer : IComputer
    {
        public string GetAppDomain()
        {
            return $"{JsonConvert.SerializeObject(GetType().Assembly.Location)}-FileName:{SomeOtherProcess.SomeThing.GetStuff()}";
        }
    }
}

