using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
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