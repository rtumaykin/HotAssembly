using NuGet;

namespace HotAssembly.Package
{
    public interface IPackageRetriever
    {
        string Retrieve(string rootPath, string packageId, SemanticVersion version);
        string Retrieve(string rootPath, string packageId);
    }
}
