using BenchmarkDotNet.Attributes;
using User.Svc;

namespace User.Benchmarks.Db;

[MemoryDiagnoser]
[SimpleJob(iterationCount: 3, warmupCount: 1)]
public class UserServiceBenchmarks
{
    private IUsersSvc _usersSvc = null!;
    private readonly string _testUserId = "1";

    [GlobalSetup]
    public void Setup()
    {
        // Sử dụng UsersSvc (in-memory) thay vì database để test nhanh hơn
        _usersSvc = new UsersSvc();
    }

    [Benchmark]
    public List<Usr> GetAllUsers()
    {
        return _usersSvc.GetAll();
    }

    [Benchmark]
    public Usr? GetUserById()
    {
        return _usersSvc.GetById(_testUserId);
    }
}
