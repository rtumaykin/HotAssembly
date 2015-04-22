using System;

namespace HotAssembly
{
    public class SqlPersistenceProvider : IPersistenceProvider
    {
        public bool GetBundle(Guid bundleId, string localPath)
        {
            throw new NotImplementedException();
        }

        public bool PersistBundle(Guid bundleId, string localPath)
        {
            throw new NotImplementedException();
        }
    }
}
