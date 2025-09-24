using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.Models;
using User.Db.MSSQL.EF.Entity;

namespace User.Test.XUnit.Db.MSSQL.EF.Cases
{
    public sealed class XPostTests : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IUsersSvc _svc;

        public XPostTests()
        {
            var services = new ServiceCollection();

            services.AddDbContext<UsrDbContext>(opt =>
                opt.UseInMemoryDatabase($"xunit-ef-{Guid.NewGuid()}"));

            services
                .AddSingleton<ITimeProvider, SystemTimeProvider>()
                .AddSingleton<IUsersSvc, EFUsrsSvc>();

            _provider = services.BuildServiceProvider();
            _svc = _provider.GetRequiredService<IUsersSvc>();
        }

        [Theory]
        [InlineData("p01", "Nguyen Van A", "123 Street, City", "1990-01-01", "Description for Nguyen Van A", true)]
        [InlineData("p02", "Tran Thi B", "456 Avenue, City", "1992-02-02", "Description for Tran Thi B", false)]
        public void Create_NewUser_Succeeds(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            var countBefore = _svc.GetAll().Count;

            var msg = _svc.Create(new UsrEtt {
                Id = id,
                FullName = fullName,
                Address = address,
                BirthDay = DateTime.Parse(birthDay),
                Description = description,
                Active = active
            });
            var countAfter = _svc.GetAll().Count;

            Assert.Null(msg);

            var found = _svc.GetById(id);
            Assert.NotNull(found);
            Assert.Equal(id, found!.Id);
            Assert.Equal(fullName, found.FullName);
            Assert.Equal(active, found.Active);

            Assert.Equal(countBefore + 1, countAfter);
        }

        [Fact]
        public void Create_DuplicateId_ReturnsError_And_NotChangeCount()
        {
            var id = "dup01";
            var newUsr = new UsrEtt { Id = id, FullName = "Vu Hieu 1", Active = true };
            var msg = _svc.Create(newUsr);
            Assert.Null(msg);

            var count = _svc.GetAll().Count;
            var msg2 = _svc.Create(new UsrEtt { Id = id, FullName = "Vu Hieu 2", Active = false });

            Assert.NotNull(msg2);
            Assert.Equal(count, _svc.GetAll().Count);
        }

        public void Dispose() => _provider.Dispose();

        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
    }
}