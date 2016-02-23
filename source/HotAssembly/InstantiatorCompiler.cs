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
                    ctor => GetInstantiator(ctor, assemblies.ToArray()));
        }

        public static IInstantiator<T> GetInstantiator(ConstructorInfo ctor, string[] referencedAssemblies)
        {
            // step 1. Compile local instantiator - the true one.
            // maybe here I will need to return (or pass) the class name to simplify the compilation
            var localInstantiator = CreateLocalInstantiator(ctor, referencedAssemblies);

            // step 2. Create a proxy class that will be sent back to main app domain as a reference.

            // step 3. Create a class that will be sent back to the main appdomain - the one that implements the interface or class methods

            var type = ctor.DeclaringType;
            var paramsInfo = ctor.GetParameters();

            var instantiatorCode =
                $"[System.Serializable]" + "\r\n" +
                $"public class instantiator_{Guid.NewGuid().ToString("N")} : System.MarshalByRefObject, HotAssembly.IInstantiator<{typeof(T).FullName}>" + "\r\n" +
                "{" + "\r\n" + 
                $"\tpublic {typeof (T).FullName} Instantiate(params object[] args) {{" + "\r\n" +
                $"\t\tvar instance = ({typeof(T).FullName})System.AppDomain.CurrentDomain.CreateInstanceAndUnwrap(" + "\r\n" +
                $"\t\t\t\"{type.Assembly.FullName}\"," + "\r\n" +
                $"\t\t\t\"{type.FullName}\"," + "\r\n" +
                "\t\t\tfalse," + "\r\n" +
                "\t\t\tSystem.Reflection.BindingFlags.Default," + "\r\n" +
                "\t\t\tnull," + "\r\n" +
                "\t\t\targs," + "\r\n" +
                "\t\t\tnull," + "\r\n" +
                "\t\t\tnull); " + "\r\n" +
                "\t\treturn instance;" + "\r\n" +
                "\t}" + "\r\n" + 
                "}";

            var compilerParameters = new CompilerParameters
            {
                CompilerOptions = "/t:library /debug",
                GenerateExecutable = false
            };

            compilerParameters.ReferencedAssemblies.AddRange(referencedAssemblies);
            compilerParameters.ReferencedAssemblies.AddRange(new []
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
                var results = codeProvider.CompileAssemblyFromSource(compilerParameters, instantiatorCode);

                var invokedProxy = (IInstantiator<T>) AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(
                    results.CompiledAssembly.Location,
                    results.CompiledAssembly.GetTypes()[0].FullName);

                return invokedProxy;
            }

        }

        private static IInstantiator<T> CreateLocalInstantiator(ConstructorInfo ctor, string[] referencedAssemblies)
        {
            throw new NotImplementedException();
        }
    }
}
