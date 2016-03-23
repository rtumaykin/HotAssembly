using System;
using UnsignedSharedType;

namespace Sample 
{
    public class Additor : ICalculator
    {
        public int Calculate(int a, int b)
        {
            // explicitly linked Json.net for .net 2.0
            Console.WriteLine($"Additor SharedType Runtime Version: {typeof(SomeType).Assembly.ImageRuntimeVersion}. Codebase: {typeof(SomeType).Assembly.CodeBase}.");
            return a + b;
        }
    }
}
