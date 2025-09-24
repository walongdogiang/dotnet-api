using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.Models;
using User.Db.MSSQL.EF.Entity;

namespace User.Test.Xut.Db.MSSQL.EF.Cases
{
    public sealed class XPutTests : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IUsersSvc _svc;

        public XPutTests()
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
        [InlineData("u01", "Vu Hieu",  "1 ABC Street", "1990-01-01", "First update",  true)]
        [InlineData("u02", "Hong Tam", "2 DEF Avenue", "1992-02-02", "Second update", false)]
        [InlineData("u03", "5ilence",  "3 GHI Road",   "1995-03-03", "Third update",  true)]
        public void Update_AllFields_AreChanged(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            if (_svc.GetById(id) == null)
            {
                var seedMsg = _svc.Create(new UsrEtt
                {
                    Id = id, FullName = "Seed Name", Address = "Seed Addr",
                    BirthDay = new DateTime(1980, 1, 1), Description = "Seed Desc", Active = !active
                });
                Assert.True(string.IsNullOrEmpty(seedMsg));
            }

            var before = _svc.GetAll().Count;

            var msg = _svc.Update(new UsrEtt
            {
                Id = id,
                FullName = fullName,
                Address = address,
                BirthDay = DateTime.Parse(birthDay),
                Description = description,
                Active = active
            });

            Assert.True(string.IsNullOrEmpty(msg));

            var found = _svc.GetById(id);
            Assert.NotNull(found);

            Assert.Equal(id, found!.Id);
            Assert.Equal(fullName, found.FullName);
            Assert.Equal(address, found.Address);
            Assert.Equal(DateTime.Parse(birthDay), found.BirthDay);
            Assert.Equal(description, found.Description);
            Assert.Equal(active, found.Active);

            Assert.Equal(before, _svc.GetAll().Count);
        }

        [Fact]
        public void Update_ExistingUser_Succeeds_And_ChangesAllFields()
        {
            const string id = "u100";
            _svc.DelById(id);
            var created = _svc.Create(new UsrEtt
            {
                Id = id, FullName = "Old Name", Address = "Old Addr",
                BirthDay = new DateTime(1999, 9, 9), Description = "Old Desc", Active = false
            });
            Assert.True(string.IsNullOrEmpty(created));

            var msg = _svc.Update(new UsrEtt
            {
                Id = id,
                FullName = "New Name",
                Address = "New Addr",
                BirthDay = new DateTime(2000, 1, 1),
                Description = "New Desc",
                Active = true
            });
            Assert.True(string.IsNullOrEmpty(msg));

            var found = _svc.GetById(id);
            Assert.NotNull(found);
            Assert.Equal("New Name", found!.FullName);
            Assert.Equal("New Addr", found.Address);
            Assert.Equal(new DateTime(2000, 1, 1), found.BirthDay);
            Assert.Equal("New Desc", found.Description);
            Assert.True(found.Active);
        }

        [Fact]
        public void Update_NotFound_ReturnsError_And_DoesNotCreate()
        {
            const string id = "u404";
            _svc.DelById(id);
            var before = _svc.GetAll().Count;

            var msg = _svc.Update(new UsrEtt
            {
                Id = id,
                FullName = "Ghost",
                Address = "No Addr",
                BirthDay = new DateTime(1970, 1, 1),
                Description = "Should fail",
                Active = false
            });

            Assert.False(string.IsNullOrEmpty(msg));
            Assert.Null(_svc.GetById(id));
            Assert.Equal(before, _svc.GetAll().Count);
        }

        public void Dispose() => _provider.Dispose();

        public interface ITimeProvider { DateTime Now { get; } }
        public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
    }
}