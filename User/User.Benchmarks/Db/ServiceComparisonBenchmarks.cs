using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Svc;
using User.Svc.Cache;
using User.Svc.Aio;

namespace User.Benchmarks.Db;

/// <summary>
/// Benchmarks để so sánh performance giữa các service implementations của IUsersSvc
/// Bao gồm: UsersSvc (in-memory), EFUsrsSvc (Entity Framework), RdsUsrSvc (Redis Cache), AioUsrSvc (All-in-One)
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 5, warmupCount: 2)]
public class ServiceComparisonBenchmarks
{
    private UsersSvc _usersSvc = null!;
    private EFUsrsSvc _efUsrsSvc = null!;
    private RdsUsrSvc _rdsUsrSvc = null!;
    private AioUsrSvc _aioUsrSvc = null!;
    private IRedisCache _redisCache = null!;
    
    private readonly string _testUserId = "benchmark_user_1";
    private readonly string _testKeyword = "benchmark";

    [GlobalSetup]
    public void Setup()
    {
        // Setup configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Server=(localdb)\\mssqllocaldb;Database=UserDb;Trusted_Connection=true;MultipleActiveResultSets=true";

        // Setup Entity Framework
        var options = new DbContextOptionsBuilder<UsrDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var dbContext = new UsrDbContext(options);

        // Setup Redis Cache Mock
        _redisCache = new MockRedisCache();

        // Initialize services
        _usersSvc = new UsersSvc();
        _efUsrsSvc = new EFUsrsSvc(dbContext);
        _rdsUsrSvc = new RdsUsrSvc(_usersSvc, _redisCache);
        _aioUsrSvc = new AioUsrSvc(_efUsrsSvc, _rdsUsrSvc, _redisCache);

        // Seed test data
        SeedTestData();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Clean up test data
        CleanupTestData();
    }

    #region GetAll Benchmarks

    [Benchmark(Baseline = true)]
    public List<Usr> UsersSvc_GetAll()
    {
        return _usersSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> EFUsrsSvc_GetAll()
    {
        return _efUsrsSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> RdsUsrSvc_GetAll()
    {
        return _rdsUsrSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_GetAll()
    {
        return _aioUsrSvc.GetAll();
    }

    #endregion

    #region GetById Benchmarks

    [Benchmark(Baseline = true)]
    public Usr? UsersSvc_GetById()
    {
        return _usersSvc.GetById(_testUserId);
    }

    [Benchmark]
    public Usr? EFUsrsSvc_GetById()
    {
        return _efUsrsSvc.GetById(_testUserId);
    }

    [Benchmark]
    public Usr? RdsUsrSvc_GetById()
    {
        return _rdsUsrSvc.GetById(_testUserId);
    }

    [Benchmark]
    public Usr? AioUsrSvc_GetById()
    {
        return _aioUsrSvc.GetById(_testUserId);
    }

    #endregion

    #region GetByKwd Benchmarks

    [Benchmark(Baseline = true)]
    public List<Usr> UsersSvc_GetByKwd()
    {
        return _usersSvc.GetByKwd(_testKeyword);
    }

    [Benchmark]
    public List<Usr> EFUsrsSvc_GetByKwd()
    {
        return _efUsrsSvc.GetByKwd(_testKeyword);
    }

    [Benchmark]
    public List<Usr> RdsUsrSvc_GetByKwd()
    {
        return _rdsUsrSvc.GetByKwd(_testKeyword);
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_GetByKwd()
    {
        return _aioUsrSvc.GetByKwd(_testKeyword);
    }

    #endregion

    #region Create Benchmarks

    [Benchmark(Baseline = true)]
    public string? UsersSvc_Create()
    {
        var user = new Usr
        {
            Id = $"benchmark_create_{Guid.NewGuid():N}",
            FullName = "Benchmark Create User",
            Active = true
        };
        return _usersSvc.Create(user);
    }

    [Benchmark]
    public string? EFUsrsSvc_Create()
    {
        var user = new Usr
        {
            Id = $"benchmark_create_{Guid.NewGuid():N}",
            FullName = "Benchmark Create User",
            Active = true
        };
        return _efUsrsSvc.Create(user);
    }

    [Benchmark]
    public string? RdsUsrSvc_Create()
    {
        var user = new Usr
        {
            Id = $"benchmark_create_{Guid.NewGuid():N}",
            FullName = "Benchmark Create User",
            Active = true
        };
        return _rdsUsrSvc.Create(user);
    }

    [Benchmark]
    public string? AioUsrSvc_Create()
    {
        var user = new Usr
        {
            Id = $"benchmark_create_{Guid.NewGuid():N}",
            FullName = "Benchmark Create User",
            Active = true
        };
        return _aioUsrSvc.Create(user);
    }

    #endregion

    #region Update Benchmarks

    [Benchmark(Baseline = true)]
    public string? UsersSvc_Update()
    {
        var user = new Usr
        {
            Id = _testUserId,
            FullName = "Updated Benchmark User",
            Active = false
        };
        return _usersSvc.Update(user);
    }

    [Benchmark]
    public string? EFUsrsSvc_Update()
    {
        var user = new Usr
        {
            Id = _testUserId,
            FullName = "Updated Benchmark User",
            Active = false
        };
        return _efUsrsSvc.Update(user);
    }

    [Benchmark]
    public string? RdsUsrSvc_Update()
    {
        var user = new Usr
        {
            Id = _testUserId,
            FullName = "Updated Benchmark User",
            Active = false
        };
        return _rdsUsrSvc.Update(user);
    }

    [Benchmark]
    public string? AioUsrSvc_Update()
    {
        var user = new Usr
        {
            Id = _testUserId,
            FullName = "Updated Benchmark User",
            Active = false
        };
        return _aioUsrSvc.Update(user);
    }

    #endregion

    #region Delete Benchmarks

    [Benchmark(Baseline = true)]
    public string? UsersSvc_Delete()
    {
        var deleteId = $"benchmark_delete_{Guid.NewGuid():N}";
        _usersSvc.Create(new Usr { Id = deleteId, FullName = "Delete User", Active = true });
        return _usersSvc.DelById(deleteId);
    }

    [Benchmark]
    public string? EFUsrsSvc_Delete()
    {
        var deleteId = $"benchmark_delete_{Guid.NewGuid():N}";
        _efUsrsSvc.Create(new Usr { Id = deleteId, FullName = "Delete User", Active = true });
        return _efUsrsSvc.DelById(deleteId);
    }

    [Benchmark]
    public string? RdsUsrSvc_Delete()
    {
        var deleteId = $"benchmark_delete_{Guid.NewGuid():N}";
        _rdsUsrSvc.Create(new Usr { Id = deleteId, FullName = "Delete User", Active = true });
        return _rdsUsrSvc.DelById(deleteId);
    }

    [Benchmark]
    public string? AioUsrSvc_Delete()
    {
        var deleteId = $"benchmark_delete_{Guid.NewGuid():N}";
        _aioUsrSvc.Create(new Usr { Id = deleteId, FullName = "Delete User", Active = true });
        return _aioUsrSvc.DelById(deleteId);
    }

    #endregion

    #region Helper Methods

    private void SeedTestData()
    {
        // Seed data for UsersSvc
        var testUsers = new[]
        {
            new Usr { Id = _testUserId, FullName = "Benchmark Test User", Active = true },
            new Usr { Id = "benchmark_user_2", FullName = "Benchmark Test User 2", Active = false },
            new Usr { Id = "benchmark_user_3", FullName = "Benchmark Test User 3", Active = true }
        };

        foreach (var user in testUsers)
        {
            _usersSvc.Create(user);
            _efUsrsSvc.Create(user);
        }
    }

    private void CleanupTestData()
    {
        // Clean up test data
        var testIds = new[] { _testUserId, "benchmark_user_2", "benchmark_user_3" };
        
        foreach (var id in testIds)
        {
            _usersSvc.DelById(id);
            _efUsrsSvc.DelById(id);
        }
    }

    #endregion
}
