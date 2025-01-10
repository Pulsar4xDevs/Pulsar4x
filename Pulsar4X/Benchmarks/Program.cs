using BenchmarkDotNet.Running;

namespace BenchmarkProject;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<Benchmarks>();
    }
}