using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet;

namespace HotAssembly.Package
{
    public interface IRetriever
    {
        string Retrieve(string packageName, SemanticVersion version);
        string Retrieve(string packageName);
    }
}
