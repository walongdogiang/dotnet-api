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
/// Benchmarks để test CRUD operations performance
/// So sánh Create, Read, Update, Delete operations giữa các service implementations
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 8, warmupCount: 2)]
public class CrudBenchmarks
{
    private UsersSvc _usersSvc = null!;
    private EFUsrsSvc _efUsrsSvc = null!;
    private RdsUsrSvc _rdsUsrSvc = null!;
    private AioUsrSvc _aioUsrSvc = null!;
    private IRedisCache _redisCache = null!;
    
    private readonly string _baseUserId = "crud_benchmark_user";

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
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Clean up test data
        CleanupTestData();
    }

    #region Create Operations Benchmarks

    [Benchmark(Baseline = true)]
    public string? UsersSvc_Create()
    {
        var user = new Usr
        {
            Id = $"users_create_{Guid.NewGuid():N}",
            FullName = "UsersSvc Create User",
            Address = "123 Create Street",
            BirthDay = DateTime.Now.AddYears(-25),
            Description = "Created by UsersSvc benchmark",
            Active = true
        };
        return _usersSvc.Create(user);
    }

    [Benchmark]
    public string? EFUsrsSvc_Create()
    {
        var user = new Usr
        {
            Id = $"ef_create_{Guid.NewGuid():N}",
            FullName = "EFUsrsSvc Create User",
            Address = "123 Create Street",
            BirthDay = DateTime.Now.AddYears(-25),
            Description = "Created by EFUsrsSvc benchmark",
            Active = true
        };
        return _efUsrsSvc.Create(user);
    }

    [Benchmark]
    public string? RdsUsrSvc_Create()
    {
        var user = new Usr
        {
            Id = $"rds_create_{Guid.NewGuid():N}",
            FullName = "RdsUsrSvc Create User",
            Address = "123 Create Street",
            BirthDay = DateTime.Now.AddYears(-25),
            Description = "Created by RdsUsrSvc benchmark",
            Active = true
        };
        return _rdsUsrSvc.Create(user);
    }

    [Benchmark]
    public string? AioUsrSvc_Create()
    {
        var user = new Usr
        {
            Id = $"aio_create_{Guid.NewGuid():N}",
            FullName = "AioUsrSvc Create User",
            Address = "123 Create Street",
            BirthDay = DateTime.Now.AddYears(-25),
            Description = "Created by AioUsrSvc benchmark",
            Active = true
        };
        return _aioUsrSvc.Create(user);
    }

    #endregion

    #region Read Operations Benchmarks

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

    [Benchmark(Baseline = true)]
    public Usr? UsersSvc_GetById()
    {
        return _usersSvc.GetById("1");
    }

    [Benchmark]
    public Usr? EFUsrsSvc_GetById()
    {
        return _efUsrsSvc.GetById("1");
    }

    [Benchmark]
    public Usr? RdsUsrSvc_GetById()
    {
        return _rdsUsrSvc.GetById("1");
    }

    [Benchmark]
    public Usr? AioUsrSvc_GetById()
    {
        return _aioUsrSvc.GetById("1");
    }

    [Benchmark(Baseline = true)]
    public List<Usr> UsersSvc_GetByKwd()
    {
        return _usersSvc.GetByKwd("Nguyen");
    }

    [Benchmark]
    public List<Usr> EFUsrsSvc_GetByKwd()
    {
        return _efUsrsSvc.GetByKwd("Nguyen");
    }

    [Benchmark]
    public List<Usr> RdsUsrSvc_GetByKwd()
    {
        return _rdsUsrSvc.GetByKwd("Nguyen");
    }

    [Benchmark]
    public List<Usr> AioUsrSvc_GetByKwd()
    {
        return _aioUsrSvc.GetByKwd("Nguyen");
    }

    #endregion

    #region Update Operations Benchmarks

    [Benchmark(Baseline = true)]
    public string? UsersSvc_Update()
    {
        var user = new Usr
        {
            Id = "1",
            FullName = "Updated UsersSvc User",
            Address = "456 Update Avenue",
            BirthDay = DateTime.Now.AddYears(-30),
            Description = "Updated by UsersSvc benchmark",
            Active = false
        };
        return _usersSvc.Update(user);
    }

    [Benchmark]
    public string? EFUsrsSvc_Update()
    {
        var user = new Usr
        {
            Id = "1",
            FullName = "Updated EFUsrsSvc User",
            Address = "456 Update Avenue",
            BirthDay = DateTime.Now.AddYears(-30),
            Description = "Updated by EFUsrsSvc benchmark",
            Active = false
        };
        return _efUsrsSvc.Update(user);
    }

    [Benchmark]
    public string? RdsUsrSvc_Update()
    {
        var user = new Usr
        {
            Id = "1",
            FullName = "Updated RdsUsrSvc User",
            Address = "456 Update Avenue",
            BirthDay = DateTime.Now.AddYears(-30),
            Description = "Updated by RdsUsrSvc benchmark",
            Active = false
        };
        return _rdsUsrSvc.Update(user);
    }

    [Benchmark]
    public string? AioUsrSvc_Update()
    {
        var user = new Usr
        {
            Id = "1",
            FullName = "Updated AioUsrSvc User",
            Address = "456 Update Avenue",
            BirthDay = DateTime.Now.AddYears(-30),
            Description = "Updated by AioUsrSvc benchmark",
            Active = false
        };
        return _aioUsrSvc.Update(user);
    }

    #endregion

    #region Delete Operations Benchmarks

    [Benchmark(Baseline = true)]
    public string? UsersSvc_Delete()
    {
        var deleteId = $"users_delete_{Guid.NewGuid():N}";
        _usersSvc.Create(new Usr { Id = deleteId, FullName = "Delete User", Active = true });
        return _usersSvc.DelById(deleteId);
    }

    [Benchmark]
    public string? EFUsrsSvc_Delete()
    {
        var deleteId = $"ef_delete_{Guid.NewGuid():N}";
        _efUsrsSvc.Create(new Usr { Id = deleteId, FullName = "Delete User", Active = true });
        return _efUsrsSvc.DelById(deleteId);
    }

    [Benchmark]
    public string? RdsUsrSvc_Delete()
    {
        var deleteId = $"rds_delete_{Guid.NewGuid():N}";
        _rdsUsrSvc.Create(new Usr { Id = deleteId, FullName = "Delete User", Active = true });
        return _rdsUsrSvc.DelById(deleteId);
    }

    [Benchmark]
    public string? AioUsrSvc_Delete()
    {
        var deleteId = $"aio_delete_{Guid.NewGuid():N}";
        _aioUsrSvc.Create(new Usr { Id = deleteId, FullName = "Delete User", Active = true });
        return _aioUsrSvc.DelById(deleteId);
    }

    #endregion

    #region Batch Operations Benchmarks

    [Benchmark(Baseline = true)]
    public int UsersSvc_BatchCreate()
    {
        var count = 0;
        for (int i = 0; i < 10; i++)
        {
            var user = new Usr
            {
                Id = $"batch_users_{i}_{Guid.NewGuid():N}",
                FullName = $"Batch UsersSvc User {i}",
                Active = i % 2 == 0
            };
            if (_usersSvc.Create(user) == null) count++;
        }
        return count;
    }

    [Benchmark]
    public int EFUsrsSvc_BatchCreate()
    {
        var count = 0;
        for (int i = 0; i < 10; i++)
        {
            var user = new Usr
            {
                Id = $"batch_ef_{i}_{Guid.NewGuid():N}",
                FullName = $"Batch EFUsrsSvc User {i}",
                Active = i % 2 == 0
            };
            if (_efUsrsSvc.Create(user) == null) count++;
        }
        return count;
    }

    [Benchmark]
    public int RdsUsrSvc_BatchCreate()
    {
        var count = 0;
        for (int i = 0; i < 10; i++)
        {
            var user = new Usr
            {
                Id = $"batch_rds_{i}_{Guid.NewGuid():N}",
                FullName = $"Batch RdsUsrSvc User {i}",
                Active = i % 2 == 0
            };
            if (_rdsUsrSvc.Create(user) == null) count++;
        }
        return count;
    }

    [Benchmark]
    public int AioUsrSvc_BatchCreate()
    {
        var count = 0;
        for (int i = 0; i < 10; i++)
        {
            var user = new Usr
            {
                Id = $"batch_aio_{i}_{Guid.NewGuid():N}",
                FullName = $"Batch AioUsrSvc User {i}",
                Active = i % 2 == 0
            };
            if (_aioUsrSvc.Create(user) == null) count++;
        }
        return count;
    }

    #endregion

    #region Complex Operations Benchmarks

    [Benchmark(Baseline = true)]
    public int UsersSvc_ComplexOperation()
    {
        // Create -> Read -> Update -> Delete cycle
        var userId = $"complex_users_{Guid.NewGuid():N}";
        
        // Create
        var createResult = _usersSvc.Create(new Usr { Id = userId, FullName = "Complex User", Active = true });
        if (createResult != null) return 0;
        
        // Read
        var user = _usersSvc.GetById(userId);
        if (user == null) return 0;
        
        // Update
        user.FullName = "Updated Complex User";
        var updateResult = _usersSvc.Update(user);
        if (updateResult != null) return 0;
        
        // Delete
        var deleteResult = _usersSvc.DelById(userId);
        return deleteResult == null ? 1 : 0;
    }

    [Benchmark]
    public int EFUsrsSvc_ComplexOperation()
    {
        // Create -> Read -> Update -> Delete cycle
        var userId = $"complex_ef_{Guid.NewGuid():N}";
        
        // Create
        var createResult = _efUsrsSvc.Create(new Usr { Id = userId, FullName = "Complex User", Active = true });
        if (createResult != null) return 0;
        
        // Read
        var user = _efUsrsSvc.GetById(userId);
        if (user == null) return 0;
        
        // Update
        user.FullName = "Updated Complex User";
        var updateResult = _efUsrsSvc.Update(user);
        if (updateResult != null) return 0;
        
        // Delete
        var deleteResult = _efUsrsSvc.DelById(userId);
        return deleteResult == null ? 1 : 0;
    }

    [Benchmark]
    public int RdsUsrSvc_ComplexOperation()
    {
        // Create -> Read -> Update -> Delete cycle
        var userId = $"complex_rds_{Guid.NewGuid():N}";
        
        // Create
        var createResult = _rdsUsrSvc.Create(new Usr { Id = userId, FullName = "Complex User", Active = true });
        if (createResult != null) return 0;
        
        // Read
        var user = _rdsUsrSvc.GetById(userId);
        if (user == null) return 0;
        
        // Update
        user.FullName = "Updated Complex User";
        var updateResult = _rdsUsrSvc.Update(user);
        if (updateResult != null) return 0;
        
        // Delete
        var deleteResult = _rdsUsrSvc.DelById(userId);
        return deleteResult == null ? 1 : 0;
    }

    [Benchmark]
    public int AioUsrSvc_ComplexOperation()
    {
        // Create -> Read -> Update -> Delete cycle
        var userId = $"complex_aio_{Guid.NewGuid():N}";
        
        // Create
        var createResult = _aioUsrSvc.Create(new Usr { Id = userId, FullName = "Complex User", Active = true });
        if (createResult != null) return 0;
        
        // Read
        var user = _aioUsrSvc.GetById(userId);
        if (user == null) return 0;
        
        // Update
        user.FullName = "Updated Complex User";
        var updateResult = _aioUsrSvc.Update(user);
        if (updateResult != null) return 0;
        
        // Delete
        var deleteResult = _aioUsrSvc.DelById(userId);
        return deleteResult == null ? 1 : 0;
    }

    #endregion

    #region Helper Methods

    private void CleanupTestData()
    {
        // Clean up any remaining test data
        // This is handled by individual operations in benchmarks
    }

    #endregion
}
