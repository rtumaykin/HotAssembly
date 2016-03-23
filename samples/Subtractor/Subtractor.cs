using System;
using UnsignedSharedType;

namespace Sample
{
    public class Subtractor : ICalculator
    {
        public int Calculate(int a, int b)
        {
            // explicitly linked Json.net for .net 4.5
            Console.WriteLine($"Subtractor SharedType Runtime Version: {typeof(SomeType).Assembly.ImageRuntimeVersion}. Codebase: {typeof(SomeType).Assembly.CodeBase}.");
            return a - b;
        }
    }
}
