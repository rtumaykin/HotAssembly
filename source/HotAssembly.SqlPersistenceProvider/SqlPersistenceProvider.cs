using System;

namespace HotAssembly
{
    public class SqlPersistenceProvider : IPersistenceProvider
    {
        public bool GetBundle(string bundleId, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public bool PersistBundle(string bundleId, string sourcePath)
        {
            throw new NotImplementedException();
        }
    }
}
