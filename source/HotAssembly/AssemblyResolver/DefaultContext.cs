using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HotAssembly.AssemblyResolver
{
    public static class DefaultContext
    {
        private static bool resolverWiredUp = false;
        public static void WireUpResolver()
        {
            if (resolverWiredUp)
                return;

            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            resolverWiredUp = true;
        }

        public static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            // only resolve when the request comes from the current AppDomain itself, 
            // or from an assembly that is in the BaseDirectory of the current AppDomain
            // todo: implement the same steps as described here: https://msdn.microsoft.com/en-us/library/yx7xezcf%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396

            // just in case if we end up with a null name
            if (args.Name == null)
                return null;

            // get either a location of a requesting assembly, or a sender AppDomain baseDirectory
            var requestorPath = string.IsNullOrWhiteSpace(args?.RequestingAssembly?.Location)
                ? (sender as AppDomain)?.BaseDirectory
                : Path.GetDirectoryName(args?.RequestingAssembly?.Location);

            // failed to get the requestor path (unlikely)
            if (string.IsNullOrWhiteSpace(requestorPath))
                return null;

            var domainBaseDirectory =
                Common.NormalizePath(AppDomain.CurrentDomain.BaseDirectory);

            // Now when we have a path, let's compare it to the current AppDomain's base directory
            if (Common.NormalizePath(requestorPath) != domainBaseDirectory)
                return null;

            // Check if this assembly has already been loaded
            var loadedAssembly =
                AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(
                        a => !string.IsNullOrWhiteSpace(a.Location) &&
                            Common.NormalizePath(Path.GetDirectoryName(a.Location)) ==
                            domainBaseDirectory && a.FullName == args.Name);
            if (loadedAssembly != null)
                return loadedAssembly;


            return Common.ResolveByFullAssemblyNameInternal(domainBaseDirectory, args.Name);
        }
    }
}
