using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotAssembly
{
    public interface IPersistenceProvider
    {
        bool GetBundle(Guid bundleId, string localPath);
        bool PersistBundle(Guid bundleId, string localPath);
    }
}
