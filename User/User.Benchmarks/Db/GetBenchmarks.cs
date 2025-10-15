using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;
using User.Svc;

namespace User.Benchmarks.Db;

[MemoryDiagnoser]
[SimpleJob]
public class GetBenchmarks
{
    private IUsersSvc _usersSvc = null!;
    private readonly string _testUserId = "1";

    [GlobalSetup]
    public void Setup()
    {
        // Đọc configuration từ appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Server=(localdb)\\mssqllocaldb;Database=UserDb;Trusted_Connection=true;MultipleActiveResultSets=true";
        
        // Khởi tạo DbContext
        var options = new DbContextOptionsBuilder<UsrDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        
        var dbContext = new UsrDbContext(options);
        
        // Khởi tạo IUsersSvc với EFUsrsSvc
        _usersSvc = new EFUsrsSvc(dbContext);
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
