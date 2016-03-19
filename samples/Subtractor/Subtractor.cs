namespace Sample
{
    public class Subtractor : ICalculator
    {
        public int Calculate(int a, int b)
        {
            return a - b;
        }
    }
}
