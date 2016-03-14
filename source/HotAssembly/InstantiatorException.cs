using System;

namespace HotAssembly
{
    [Serializable]
    public class InstantiatorException : Exception
    {
        public InstantiatorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
