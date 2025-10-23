using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Svc;
using User.Svc.Cache;
using User.Svc.Aio;

namespace User.Benchmarks.Db;

/// <summary>
/// Benchmarks để test cache performance và behavior
/// So sánh cache hit vs cache miss, cache invalidation, và cache rebuild performance
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 3)]
public class CacheBenchmarks
{
    private RdsUsrSvc _rdsUsrSvc = null!;
    private AioUsrSvc _aioUsrSvc = null!;
    private IRedisCache _redisCache = null!;
    private EFUsrsSvc _efUsrsSvc = null!;
    
    private readonly string _testUserId = "cache_benchmark_user";
    private readonly string _testKeyword = "cache";

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
        var usersSvc = new UsersSvc();
        _efUsrsSvc = new EFUsrsSvc(dbContext);
        _rdsUsrSvc = new RdsUsrSvc(usersSvc, _redisCache);
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

    #region Cache Hit vs Miss Benchmarks

    [Benchmark(Baseline = true)]
    public Usr? RdsUsrSvc_GetById_CacheMiss()
    {
        // Clear cache to ensure cache miss
        _redisCache.Remove($"user:id:{_testUserId}");
        return _rdsUsrSvc.GetById(_testUserId);
    }

    [Benchmark]
    public Usr? RdsUsrSvc_GetById_CacheHit()
    {
        // Ensure cache hit by calling GetById first
        _rdsUsrSvc.GetById(_testUserId);
        return _rdsUsrSvc.GetById(_testUserId);
    }

    [Benchmark]
    public Usr? AioUsrSvc_GetById_CacheMiss()
    {
        // Clear cache to ensure cache miss
        _redisCache.Remove($"aio:user:id:{_testUserId}");
        return _aioUsrSvc.GetById(_testUserId);
    }

    [Benchmark]
    public Usr? AioUsrSvc_GetById_CacheHit()
    {
        // Ensure cache hit by calling GetById first
        _aioUsrSvc.GetById(_testUserId);
        return _aioUsrSvc.GetById(_testUserId);
    }

    #endregion

    #region GetAll Cache Benchmarks

    [Benchmark(Baseline = true)]
    public List<Usr> RdsUsrSvc_GetAll_CacheMiss()
    {
        // Clear cache to ensure cache miss
        _redisCache.Remove("users:all");
        return _rdsUsrSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> RdsUsrSvc_GetAll_CacheHit()
    {
        // Ensure cache hit by calling GetAll first
        _rdsUsrSvc.GetAll();
        return _rdsUsrSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_GetAll_CacheMiss()
    {
        // Clear cache to ensure cache miss
        _redisCache.Remove("aio:users:all");
        return _aioUsrSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_GetAll_CacheHit()
    {
        // Ensure cache hit by calling GetAll first
        _aioUsrSvc.GetAll();
        return _aioUsrSvc.GetAll();
    }

    #endregion

    #region GetByKwd Cache Benchmarks

    [Benchmark(Baseline = true)]
    public List<Usr> RdsUsrSvc_GetByKwd_CacheMiss()
    {
        // Clear cache to ensure cache miss
        _redisCache.Remove($"users:keyword:{_testKeyword}");
        return _rdsUsrSvc.GetByKwd(_testKeyword);
    }

    [Benchmark]
    public List<Usr> RdsUsrSvc_GetByKwd_CacheHit()
    {
        // Ensure cache hit by calling GetByKwd first
        _rdsUsrSvc.GetByKwd(_testKeyword);
        return _rdsUsrSvc.GetByKwd(_testKeyword);
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_GetByKwd_CacheMiss()
    {
        // Clear cache to ensure cache miss
        _redisCache.Remove($"aio:users:keyword:{_testKeyword}");
        return _aioUsrSvc.GetByKwd(_testKeyword);
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_GetByKwd_CacheHit()
    {
        // Ensure cache hit by calling GetByKwd first
        _aioUsrSvc.GetByKwd(_testKeyword);
        return _aioUsrSvc.GetByKwd(_testKeyword);
    }

    #endregion

    #region Cache Invalidation Benchmarks

    [Benchmark(Baseline = true)]
    public string? RdsUsrSvc_Create_WithCacheInvalidation()
    {
        var user = new Usr
        {
            Id = $"cache_inv_create_{Guid.NewGuid():N}",
            FullName = "Cache Invalidation Create User",
            Active = true
        };
        return _rdsUsrSvc.Create(user);
    }

    [Benchmark]
    public string? RdsUsrSvc_Update_WithCacheInvalidation()
    {
        var user = new Usr
        {
            Id = _testUserId,
            FullName = "Updated Cache Invalidation User",
            Active = false
        };
        return _rdsUsrSvc.Update(user);
    }

    [Benchmark]
    public string? AioUsrSvc_Create_WithCacheInvalidation()
    {
        var user = new Usr
        {
            Id = $"cache_inv_create_{Guid.NewGuid():N}",
            FullName = "Cache Invalidation Create User",
            Active = true
        };
        return _aioUsrSvc.Create(user);
    }

    [Benchmark]
    public string? AioUsrSvc_Update_WithCacheInvalidation()
    {
        var user = new Usr
        {
            Id = _testUserId,
            FullName = "Updated Cache Invalidation User",
            Active = false
        };
        return _aioUsrSvc.Update(user);
    }

    #endregion

    #region Cache Rebuild Benchmarks

    [Benchmark(Baseline = true)]
    public List<Usr> RdsUsrSvc_CacheRebuild_AfterInvalidation()
    {
        // Invalidate cache
        _redisCache.Remove("users:all");
        
        // Rebuild cache
        return _rdsUsrSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_CacheRebuild_AfterInvalidation()
    {
        // Invalidate cache
        _redisCache.Remove("aio:users:all");
        
        // Rebuild cache
        return _aioUsrSvc.GetAll();
    }

    #endregion

    #region Cache Warm-up Benchmarks

    [Benchmark(Baseline = true)]
    public void RdsUsrSvc_CacheWarmUp()
    {
        // Clear all caches
        _redisCache.Remove("users:all");
        _redisCache.Remove($"user:id:{_testUserId}");
        _redisCache.Remove($"users:keyword:{_testKeyword}");
        
        // Warm up caches
        _rdsUsrSvc.GetAll();
        _rdsUsrSvc.GetById(_testUserId);
        _rdsUsrSvc.GetByKwd(_testKeyword);
    }

    [Benchmark]
    public void AioUsrSvc_CacheWarmUp()
    {
        // Clear all caches
        _redisCache.Remove("aio:users:all");
        _redisCache.Remove($"aio:user:id:{_testUserId}");
        _redisCache.Remove($"aio:users:keyword:{_testKeyword}");
        
        // Warm up caches
        _aioUsrSvc.GetAll();
        _aioUsrSvc.GetById(_testUserId);
        _aioUsrSvc.GetByKwd(_testKeyword);
    }

    #endregion

    #region Cache Performance Comparison

    [Benchmark(Baseline = true)]
    public Usr? DirectDatabase_GetById()
    {
        return _efUsrsSvc.GetById(_testUserId);
    }

    [Benchmark]
    public Usr? RdsUsrSvc_GetById_Performance()
    {
        return _rdsUsrSvc.GetById(_testUserId);
    }

    [Benchmark]
    public Usr? AioUsrSvc_GetById_Performance()
    {
        return _aioUsrSvc.GetById(_testUserId);
    }

    [Benchmark]
    public List<Usr> DirectDatabase_GetAll()
    {
        return _efUsrsSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> RdsUsrSvc_GetAll_Performance()
    {
        return _rdsUsrSvc.GetAll();
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_GetAll_Performance()
    {
        return _aioUsrSvc.GetAll();
    }

    #endregion

    #region Helper Methods

    private void SeedTestData()
    {
        // Seed data for testing
        var testUsers = new[]
        {
            new Usr { Id = _testUserId, FullName = "Cache Benchmark Test User", Active = true },
            new Usr { Id = "cache_user_2", FullName = "Cache Benchmark Test User 2", Active = false },
            new Usr { Id = "cache_user_3", FullName = "Cache Benchmark Test User 3", Active = true }
        };

        foreach (var user in testUsers)
        {
            _efUsrsSvc.Create(user);
        }
    }

    private void CleanupTestData()
    {
        // Clean up test data
        var testIds = new[] { _testUserId, "cache_user_2", "cache_user_3" };
        
        foreach (var id in testIds)
        {
            _efUsrsSvc.DelById(id);
        }
    }

    #endregion
}
