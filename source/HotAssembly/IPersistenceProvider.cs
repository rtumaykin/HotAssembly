using System;

namespace HotAssembly
{
    public interface IPersistenceProvider
    {
        bool GetBundle(string bundleId, string destinationPath);
        bool PersistBundle(string bundleId, string sourcePath);
    }
}
