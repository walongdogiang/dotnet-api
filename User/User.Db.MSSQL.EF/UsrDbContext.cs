using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using User.Db.MSSQL.EF.Entity;

namespace User.Db.MSSQL.EF;

public class UsrDbContext : DbContext
{
    public UsrDbContext(DbContextOptions<UsrDbContext> options) : base(options) { }

    public DbSet<UsrEtt> Users => Set<UsrEtt>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
    }
}
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UsrDbContext>
{
    public UsrDbContext CreateDbContext(string[] args)
    {
        // Load configuration from appsettings.json and User Secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "User.Api")) // Look in User.Api project
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Add appsettings.json
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true) // Add appsettings.Development.json
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true) // Add User Secrets
            .AddEnvironmentVariables() // Add environment variables
            .Build();

        // Get connection string
        var cs = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing connection string 'Default'.");

        // Configure DbContextOptions
        var opt = new DbContextOptionsBuilder<UsrDbContext>()
            .UseSqlServer(cs)
            .Options;

        return new UsrDbContext(opt);
    }
}