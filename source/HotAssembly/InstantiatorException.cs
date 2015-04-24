using System;

namespace HotAssembly
{
    public class InstantiatorException : Exception
    {
        public InstantiatorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
