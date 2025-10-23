using BenchmarkDotNet.Running;
using User.Benchmarks.Db;

namespace User.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("🚀 Starting User Service Benchmarks...");
        Console.WriteLine("=====================================");
        
        // Parse command line arguments
        var benchmarkType = args.Length > 0 ? args[0].ToLower() : "all";
        
        switch (benchmarkType)
        {
            case "simple":
                Console.WriteLine("📊 Running Simple Benchmarks...");
                BenchmarkRunner.Run<SimpleBenchmarks>();
                break;
                
            case "service":
                Console.WriteLine("📊 Running Service Comparison Benchmarks...");
                BenchmarkRunner.Run<ServiceComparisonBenchmarks>();
                break;
                
            case "cache":
                Console.WriteLine("📊 Running Cache Benchmarks...");
                BenchmarkRunner.Run<CacheBenchmarks>();
                break;
                
            case "crud":
                Console.WriteLine("📊 Running CRUD Benchmarks...");
                BenchmarkRunner.Run<CrudBenchmarks>();
                break;
                
            case "all":
            default:
                Console.WriteLine("📊 Running All Benchmarks...");
                Console.WriteLine();
                
                Console.WriteLine("1️⃣ Simple Benchmarks...");
                BenchmarkRunner.Run<SimpleBenchmarks>();
                Console.WriteLine();
                
                Console.WriteLine("2️⃣ Service Comparison Benchmarks...");
                BenchmarkRunner.Run<ServiceComparisonBenchmarks>();
                Console.WriteLine();
                
                Console.WriteLine("3️⃣ Cache Benchmarks...");
                BenchmarkRunner.Run<CacheBenchmarks>();
                Console.WriteLine();
                
                Console.WriteLine("4️⃣ CRUD Benchmarks...");
                BenchmarkRunner.Run<CrudBenchmarks>();
                break;
        }
        
        Console.WriteLine("✅ All benchmarks completed!");
        Console.WriteLine("=====================================");
    }
}
