using NuGet;

namespace HotAssembly
{
    public class InstantiatorKey
    {
        public string PackageId { get;}

        public string Version { get; }

        public string FullTypeName { get;}

        public override bool Equals(object obj)
        {
            var typedObj = obj as InstantiatorKey;

            return typedObj != null && typedObj.PackageId == PackageId && typedObj.Version == Version && typedObj.FullTypeName == FullTypeName;
        }

        public override string ToString()
        {
            return $"{PackageId}.{Version}.{FullTypeName}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public InstantiatorKey(string packageId, string version, string fullTypeName)
        {
            PackageId = packageId;
            Version = SemanticVersion.Parse(version).ToNormalizedString(); 
            FullTypeName = fullTypeName;
        }
    }
}
