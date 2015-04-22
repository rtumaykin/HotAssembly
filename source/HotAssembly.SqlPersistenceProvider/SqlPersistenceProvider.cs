using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
