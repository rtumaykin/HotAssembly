using System;

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
