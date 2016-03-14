using System;

namespace HotAssembly
{
    [Serializable]
    public class InstantiatorCreationException : Exception
    {
        public bool IsFatal { get; private set; }
        public InstantiatorCreationException(string message, Exception originalException, bool isFatal)
            : base(message, originalException)
        {
            IsFatal = isFatal;
        }
    }
}
