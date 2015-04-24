namespace HotAssembly
{
    public interface IPersistenceProvider
    {
        void GetBundle(string bundleId, string destinationPath);
        void PersistBundle(string bundleId, string sourcePath);
    }
}
