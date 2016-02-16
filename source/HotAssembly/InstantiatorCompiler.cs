using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotAssembly.Package;
using Newtonsoft.Json;
using NuGet;

namespace HotAssembly
{
    public delegate T Instantiator<T>(params object[] args);

    public class InstantiatorCompiler<T> : MarshalByRefObject
    {
        private string _destinationPath;
        private string _packageId;
        private SemanticVersion _semanticVersion;
        private IPackageRetriever _packageRetriever;


        public InstantiatorCompiler(string destinationPath, IPackageRetriever packageRetriever, string packageId, SemanticVersion semanticVersion)
        {
            _destinationPath = destinationPath;
            _packageRetriever = packageRetriever;
            _packageId = packageId;
            _semanticVersion = semanticVersion;
        }

        public Dictionary<string, Instantiator<T>> CreateInstantiatorsForPackage()
        {
            string packagePath;
            Directory.CreateDirectory(_destinationPath);

            try
            {
                packagePath = _packageRetriever.Retrieve(_destinationPath, _packageId, _semanticVersion);
            }
            catch (Exception e)
            {
                throw new InstantiatorCreationException($"Package Retriever Failed to obtain the package {_packageId}.{_semanticVersion.ToNormalizedString()}", e, true);
            }

            if (string.IsNullOrWhiteSpace(packagePath))
                throw new InstantiatorCreationException($"Package Retriever Failed to obtain the package {_packageId}.{_semanticVersion.ToNormalizedString()} from available sources", null, true);


            var manifestPath = Path.Combine(packagePath, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new InstantiatorCreationException($"Could not find manifest at \"{manifestPath}\"", null, true);

            var manifest =
                JsonConvert.DeserializeObject<PackageManifest>(File.ReadAllText(Path.Combine(packagePath, "manifest.json")));

            // find the directory where the dlls are
            var libPath = Directory.GetDirectories(Path.Combine(packagePath, "lib")).FirstOrDefault() ??
                          Path.Combine(packagePath, "lib");

            Assembly instanceAssembly = null;
            foreach (
                var file in
                    Directory.GetFiles(libPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                if (Path.GetFileName(file) == manifest.ClassAssemblyName)
                {
                    var assembly = Assembly.LoadFrom(file);
                    instanceAssembly = assembly;
                }
            }

            if (instanceAssembly == null)
                throw new InstantiatorCreationException(
                    $"There was no assembly found on this path: \"{packagePath}\"", null, true);


            var instanceRealType =
                instanceAssembly.DefinedTypes.FirstOrDefault(
                    info => info.IsClass && info.FullName == manifest.FullyQualifiedClassName);

            if (instanceRealType == null)
                throw new InstantiatorCreationException(
                    $"Type \"{manifest.FullyQualifiedClassName}\" was not found in assembly \"{Path.Combine(packagePath, manifest.FullyQualifiedClassName)}\"", null, true);

            // for siimplicity I only want the initialization done through a single object.  In the future I might expand to any type of constructor
            var ctors = instanceRealType.GetConstructors();

            if (ctors == null || !ctors.Any())
                throw new InstantiatorCreationException(
                    $"No public constructors for type {instanceRealType} were found", null, true);

            return
                ctors.ToDictionary(
                ctor => !ctor.GetParameters().Any() ? "" : string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.FullName)),
                    ctor => GetInstantiator<T>(ctor));
        }
        public static Instantiator<T> GetInstantiator<T>(ConstructorInfo ctor)
        {
            var type = ctor.DeclaringType;
            var paramsInfo = ctor.GetParameters();

            //create a single param of type object[]
            var param =
                Expression.Parameter(typeof(object[]), "args");

            var argsExp =
                new Expression[paramsInfo.Length];

            //pick each arg from the params array 
            //and create a typed expression of them
            for (var i = 0; i < paramsInfo.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = paramsInfo[i].ParameterType;

                var paramAccessorExp =
                    Expression.ArrayIndex(param, index);

                var paramCastExp =
                    Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            //make a NewExpression that calls the
            //ctor with the args we just created
            var newExp = Expression.New(ctor, argsExp);

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            //            var lambda = Expression.Lambda<Instantiator<T>>(Expression.Convert(newExp, typeof(T)), param);
            var lambda = Expression.Lambda<Instantiator<T>>(newExp, param);

            //compile it
            var compiled = lambda.Compile();
            return compiled;
        }

    }
}
