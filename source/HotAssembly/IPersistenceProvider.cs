using System;

namespace HotAssembly
{
    public interface IPersistenceProvider
    {
        bool GetBundle(Guid bundleId, string localPath);
        bool PersistBundle(Guid bundleId, string localPath);
    }
}
