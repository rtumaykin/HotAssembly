using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp;

namespace HotAssembly
{
    internal class Compiler : MarshalByRefObject
    {
        public IEnumerable<CompilerError> Compile(string code, bool checkSyntaxOnly, string assemblyFullPath, string[] referencedAssemblies)
        {
            var result = new List<CompilerError>();

            var compilerParameters = new CompilerParameters
            {
                CompilerOptions = "/t:library",
                GenerateExecutable = false,
                GenerateInMemory = checkSyntaxOnly
            };
            compilerParameters.ReferencedAssemblies.AddRange(referencedAssemblies);
            if (!checkSyntaxOnly)
                compilerParameters.OutputAssembly = assemblyFullPath;

            var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };
            CodeDomProvider codeProvider = new CSharpCodeProvider(providerOptions);

            using (codeProvider)
            {
                var results = codeProvider.CompileAssemblyFromSource(compilerParameters, code);

                if (results.Errors.Count > 0)
                {
                    result.AddRange(results.Errors.Cast<CompilerError>());
                }
            }

            return result;
        }
    }
}
