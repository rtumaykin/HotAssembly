using System;
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

    public class Computer1 : IComputer
    {
        public string GetAppDomain()
        {
            return $"{JsonConvert.SerializeObject(GetType().Assembly.Location)}-//{GetType()}!!!//FileName:{SomeOtherProcess.SomeThing.GetStuff()}";
        }
    }
}

