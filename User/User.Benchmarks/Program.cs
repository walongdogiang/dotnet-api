using BenchmarkDotNet.Running;
using User.Benchmarks.Db;

namespace User.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Chạy SimpleBenchmarks
        var summary = BenchmarkRunner.Run<SimpleBenchmarks>();
    }
}
