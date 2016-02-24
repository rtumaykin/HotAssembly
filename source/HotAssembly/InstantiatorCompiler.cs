using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HotAssembly.Package;
using Microsoft.CSharp;
using Newtonsoft.Json;
using NuGet;
using NuGet.Runtime;

namespace HotAssembly
{
    public class InstantiatorCompiler<T> : MarshalByRefObject where T:class 
    {
        private readonly string _classAssemblyName;
        private readonly string _fullyQualifiedClassName;


        public InstantiatorCompiler(string classAssemblyName, string fullyQualifiedClassName)
        {
            _classAssemblyName = classAssemblyName;
            _fullyQualifiedClassName = fullyQualifiedClassName;
        }

        public Dictionary<string, IInstantiator<T>> CreateInstantiatorsForPackage()
        {
            var assemblies = Directory.GetFileSystemEntries(AppDomain.CurrentDomain.BaseDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            var instanceAssemblyPath = assemblies.FirstOrDefault(a => Path.GetFileName(a) == _classAssemblyName);
            if (string.IsNullOrWhiteSpace(instanceAssemblyPath))
                throw new InstantiatorCreationException(
                    $"There was no assembly found on this path: \"{AppDomain.CurrentDomain.BaseDirectory}\"", null, true);

            var instanceAssembly = Assembly.LoadFrom(instanceAssemblyPath);

            var instanceRealType =
                instanceAssembly.DefinedTypes.FirstOrDefault(
                    info => info.IsClass && info.FullName == _fullyQualifiedClassName);

            if (instanceRealType == null)
                throw new InstantiatorCreationException(
                    $"Type \"{_fullyQualifiedClassName}\" was not found in assembly \"{instanceAssemblyPath}\"", null, true);

            var ctors = instanceRealType.GetConstructors();

            if (ctors == null || !ctors.Any())
                throw new InstantiatorCreationException(
                    $"No public constructors for type {instanceRealType} were found", null, true);

            return
                ctors.ToDictionary(
                ctor => !ctor.GetParameters().Any() ? "" : string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.FullName)),
                    ctor => GetInstantiatorBundle(ctor, assemblies.ToArray()));
        }

        public static IInstantiator<T> GetInstantiatorBundle(ConstructorInfo ctor, string[] referencedAssemblies)
        {
            var randomizer = Guid.NewGuid().ToString("N");
            // step 1. Compile local instantiator - the true one.
            // maybe here I will need to return (or pass) the class name to simplify the compilation
            //var localInstantiator = CreateLocalInstantiator(ctor, referencedAssemblies, randomizer);
            // todo: add it to a static collection inside local AppDomain


            // step 2. Create a proxy class that will be sent back to main app domain as a reference.
            var remoteProxy = CreateRemoteProxyObject(ctor, referencedAssemblies, randomizer);

            return remoteProxy;
        }

        private static string CodeLine(string line, int ident)
        {
            var result = "";
            for (var i = 0; i < ident; i++)
            {
                result += "\t";
            }
            result += line;
            result += "\r\n";
            return result;
        }

        private static IInstantiator<T> CreateRemoteProxyObject(ConstructorInfo ctor, string[] referencedAssemblies, string randomizer)
        {
            var type = ctor.DeclaringType;
            var paramsInfo = ctor.GetParameters();

            var code =
                CodeLine($"namespace ns_{randomizer} {{", 0) +
                CodeLine($"public class RemoteInstantiator_{randomizer} : System.MarshalByRefObject", 1) +
                CodeLine("{", 1) +
                CodeLine(
                    $"private readonly System.Collections.Concurrent.ConcurrentDictionary<System.Guid, {typeof (T).FullName}> _instances = new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, {typeof (T).FullName}>();",
                    2) +
                CodeLine("public System.Guid CreateInstance(params object[] args)", 2) +
                CodeLine("{", 2) +
                CodeLine("var instanceHandle = System.Guid.NewGuid();", 3) +
                CodeLine($"if (_instances.TryAdd(instanceHandle, new {type.FullName}(args)))", 3) + //todo: make this constructor specific
                CodeLine("{", 3) +
                CodeLine("return instanceHandle;", 4) +
                CodeLine("}", 3) +
                CodeLine("return System.Guid.Empty;", 3) +
                CodeLine("}", 2) +
                CodeLine("public void DisposeInstance(System.Guid instanceHandle)", 2) +
                CodeLine("{", 2) +
                CodeLine($"{typeof (T).FullName} instance;", 3) +
                CodeLine("_instances.TryRemove(instanceHandle, out instance);", 3) +
                CodeLine("}", 2);

            // copy all public methods of the passed class/interface
            foreach (var methodInfo in ctor.DeclaringType.GetMethods())
            {
                // todo: later add handling of the generics and defaults
                var callParameters = $"{string.Join(", ", methodInfo.GetParameters().Select(p => $"{p.Name}"))}";

                var localParameters = $"(System.Guid instanceHandle{(methodInfo.GetParameters().Any() ? "," : "")} {string.Join(", ", methodInfo.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"))})";

                // todo: test if void
                var body =
                    CodeLine($"public {methodInfo.ReturnType.FullName} {methodInfo.Name} {localParameters}", 2) +
                    CodeLine("{", 2) +
                    CodeLine($"{typeof(T)} instance;", 3) +
                    CodeLine("if (_instances.TryGetValue(instanceHandle, out instance))", 3) +
                    CodeLine("{", 3) +
                    CodeLine($"return instance.{methodInfo.Name} ({callParameters});", 4) +
                    CodeLine("}", 3) +
                    CodeLine($"return default({methodInfo.ReturnType.FullName});", 3) +
                    CodeLine("}", 2);

                code += body;
            }

            code +=
                CodeLine("}", 1) +
                CodeLine("[System.Serializable]", 1) +
                CodeLine($"public class LocalProxy_{randomizer} : {typeof (T).FullName}, System.IDisposable", 1) +
                CodeLine("{", 1) +
                CodeLine($"private readonly RemoteInstantiator_{randomizer} _remoteInstantiator;", 2) +
                CodeLine("private readonly System.Guid _remoteInstanceHandle;", 2) +
                CodeLine(
                    $"public LocalProxy_{randomizer} (RemoteInstantiator_{randomizer} remoteInstantiator, System.Guid remoteInstanceHandle)",
                    2) +
                CodeLine("{", 2) +
                CodeLine("_remoteInstantiator = remoteInstantiator;", 3) +
                CodeLine("_remoteInstanceHandle = remoteInstanceHandle;", 3) +
                CodeLine("}", 2) +
                CodeLine("public void Dispose()", 2) +
                CodeLine("{", 2) +
                CodeLine("_remoteInstantiator?.DisposeInstance(_remoteInstanceHandle);", 3) +
                CodeLine("}", 2);

            // copy all public methods of the passed class/interface
            foreach (var methodInfo in ctor.DeclaringType.GetMethods())
            {
                // todo: later add handling of the generics and defaults
                var parameters =
                    $"{string.Join(", ", methodInfo.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"))}";

                var callParameters = $"_remoteInstanceHandle{(methodInfo.GetParameters().Any() ? "," : "")} {string.Join(", ", methodInfo.GetParameters().Select(p => $"{p.Name}"))}";

                // todo: test if void
                var body =
                    CodeLine($"public {methodInfo.ReturnType.FullName} {methodInfo.Name} ({parameters})", 2) +
                    CodeLine("{", 2) +
                    CodeLine($"return _remoteInstantiator.{methodInfo.Name}({callParameters});", 3) +
                    CodeLine("}", 2);

                code += body;
            }

            code += 
                CodeLine("}", 1) +
                CodeLine("[System.Serializable]", 1) +
                CodeLine($"public class LocalInstantiator_{randomizer} : HotAssembly.IInstantiator<{typeof(T).FullName}>", 1) +
                CodeLine("{", 1) +
                CodeLine($"private readonly RemoteInstantiator_{randomizer} _remoteInstantiator;", 2) +
                CodeLine($"public LocalInstantiator_{randomizer} (RemoteInstantiator_{randomizer} remoteInstantiator)", 2) +
                CodeLine("{", 2) +
                CodeLine("_remoteInstantiator = remoteInstantiator", 3) +
                CodeLine("}", 2) +
                CodeLine($"public {typeof(T).FullName} Instantiate(params object[] args)", 2) +
                CodeLine("{", 2) +
                CodeLine("var remoteInstanceHandle = _remoteInstantiator.CreateInstance(args);", 3) +
                CodeLine($"var instance = new LocalProxy_{randomizer} (_remoteInstantiator, remoteInstanceHandle);", 3) +
                CodeLine("return instance;", 3) +
                CodeLine("}", 2) +
                CodeLine("}", 1) +
                CodeLine("}", 0);

            var compilerParameters = new CompilerParameters
            {
                CompilerOptions = "/t:library /debug",
                GenerateExecutable = false
            };

            compilerParameters.ReferencedAssemblies.AddRange(referencedAssemblies);
            compilerParameters.ReferencedAssemblies.AddRange(new[]
            {
                typeof(T).Assembly.Location,
                typeof(IInstantiator<T>).Assembly.Location,
                "System.Core.dll",
                "mscorlib.dll",
                "System.dll"
            });
            compilerParameters.OutputAssembly = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dll");

            var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };
            CodeDomProvider codeProvider = new CSharpCodeProvider(providerOptions);

            using (codeProvider)
            {
                var results = codeProvider.CompileAssemblyFromSource(compilerParameters, code);

                var remoteInstantiator = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(
                    results.CompiledAssembly.Location,
                    $"ns_{randomizer}.RemoteInstantiator_{randomizer}");

                var localInstantiatorConstructor =
                    results.CompiledAssembly.DefinedTypes
                        .FirstOrDefault(dt => dt.FullName == $"ns_{randomizer}.LocalInstantiator_{randomizer}")?
                        .GetConstructors()
                        .FirstOrDefault();
                var localInstantiator = (IInstantiator<T>) localInstantiatorConstructor.Invoke(new [] { remoteInstantiator});
                return localInstantiator;
            }

        }

    }
}
