using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.Models;
using User.Db.MSSQL.EF.Entity;

namespace User.Test.Xut.Db.MSSQL.EF.Cases
{
    public sealed class XDelTests : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IUsersSvc _svc;

        public XDelTests()
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
        [InlineData("d01")]
        [InlineData("d02")]
        public void Delete_ExistingUser_Succeeds(string id)
        {
            var newUser = new UsrEtt { Id = id, FullName = "Temp", Active = true };
            Assert.Null(_svc.Create(newUser));
            var countBefore = _svc.GetAll().Count;

            var msg = _svc.DelById(id);

            Assert.Null(msg);
            Assert.Equal(countBefore - 1, _svc.GetAll().Count);
            Assert.Null(_svc.GetById(id));
        }

        [Fact]
        public void Delete_NotFound_ReturnsError_And_KeepCount()
        {
            var before = _svc.GetAll().Count;

            var msg = _svc.DelById("abc100");

            Assert.NotNull(msg);
            Assert.Equal(before, _svc.GetAll().Count);
        }

        [Fact]
        public void Delete_AllUsers_EmptiesCollection()
        {
            if (_svc.GetAll().Count == 0)
            {
                _svc.Create(new UsrEtt { Id = "a1", FullName = "A", Active = true });
                _svc.Create(new UsrEtt { Id = "a2", FullName = "B", Active = true });
                _svc.Create(new UsrEtt { Id = "a3", FullName = "C", Active = true });
            }

            var list = _svc.GetAll();
            Assert.NotNull(list);
            Assert.True(list.Count >= 3);

            foreach (var u in list.ToArray())
            {
                var del = _svc.DelById(u.Id);
                Assert.Null(del);
            }

            Assert.Equal(0, _svc.GetAll().Count);
        }

        public void Dispose() => _provider.Dispose();

        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
    }
}