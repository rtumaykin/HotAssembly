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
            return $"{JsonConvert.SerializeObject(GetType().Assembly.Location)}-//{GetType()}!!!//FileName:{SomeOtherProcess.SomeThing.GetStuff()}";
        }
    }

    [HotAssembly]
    public class Computer1 : IComputer
    {
        public string GetAppDomain()
        {
            return $"{JsonConvert.SerializeObject(GetType().Assembly.Location)}-//{GetType()}!!!//FileName:{SomeOtherProcess.SomeThing.GetStuff()}";
        }
    }
}

