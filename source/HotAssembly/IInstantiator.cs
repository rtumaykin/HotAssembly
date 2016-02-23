using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotAssembly
{
    public interface IInstantiator<T> where T:class
    {
        T Instantiate(params object[] args);
    }
}
