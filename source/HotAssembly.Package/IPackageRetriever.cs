using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet;

namespace HotAssembly.Package
{
    public interface IPackageRetriever
    {
        string Retrieve(string rootPath, string packageId, SemanticVersion version);
        string Retrieve(string rootPath, string packageId);
    }
}
