using BenchmarkDotNet.Attributes;
using User.Svc;

namespace User.Benchmarks.Db;

[MemoryDiagnoser]
[SimpleJob(iterationCount: 1, warmupCount: 0)]
public class SimpleBenchmarks
{
    private IUsersSvc _usersSvc = null!;
    private readonly string _testUserId = "1";

    [GlobalSetup]
    public void Setup()
    {
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
