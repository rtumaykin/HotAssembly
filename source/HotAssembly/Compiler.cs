using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.CSharp;

namespace HotAssembly
{
    internal class Compiler : MarshalByRefObject
    {
        public CompilerResults Compile(string code, bool checkSyntaxOnly, string outputAssembly, string[] referencedAssemblies)
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
                compilerParameters.OutputAssembly = outputAssembly;

            var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };

            using (CodeDomProvider codeProvider = new CSharpCodeProvider(providerOptions))
            {
                return codeProvider.CompileAssemblyFromSource(compilerParameters, code);
            }
        }
    }
}
