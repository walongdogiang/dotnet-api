using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.Models;
using User.Db.MSSQL.EF.Entity;

namespace User.Test.Nun.Db.MSSQL.EF.Cases
{
    [TestFixture]
    public sealed class NPutTests
    {
        private IUsersSvc _svc = null!;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();

            // DbContext InMemory: mỗi test class 1 DB riêng bằng Guid
            services.AddDbContext<UsrDbContext>(opt =>
                opt.UseInMemoryDatabase($"nunit-ef-{Guid.NewGuid()}"));

            // Đăng ký EFUsrsSvc thay vì UsersSvc
            services
                .AddSingleton<ITimeProvider, SystemTimeProvider>()
                .AddSingleton<IUsersSvc, EFUsrsSvc>();

            var provider = services.BuildServiceProvider();
            _svc = provider.GetRequiredService<IUsersSvc>();
        }

        [TestCase("u01", "Vu Hieu",  "1 ABC Street", "1990-01-01", "First update",  true)]
        [TestCase("u02", "Hong Tam", "2 DEF Avenue", "1992-02-02", "Second update", false)]
        [TestCase("u03", "5ilence",  "3 GHI Road",   "1995-03-03", "Third update",  true)]
        public void Update_AllFields_AreChanged(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            // Seed nếu chưa có
            if (_svc.GetById(id) == null)
            {
                var seedMsg = _svc.Create(new UsrEtt
                {
                    Id = id,
                    FullName = "Seed Name",
                    Address = "Seed Addr",
                    BirthDay = new DateTime(1980, 1, 1),
                    Description = "Seed Desc",
                    Active = !active
                });
                Assert.That(string.IsNullOrEmpty(seedMsg), Is.True, $"Seeding '{id}' should succeed.");
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

            Assert.That(msg, Is.Null);

            var found = _svc.GetById(id);
            Assert.That(found, Is.Not.Null, "User should exist after update.");

            Assert.That(found!.Id, Is.EqualTo(id));
            Assert.That(found.FullName, Is.EqualTo(fullName));
            Assert.That(found.Address, Is.EqualTo(address));
            Assert.That(found.BirthDay, Is.EqualTo(DateTime.Parse(birthDay)));
            Assert.That(found.Description, Is.EqualTo(description));
            Assert.That(found.Active, Is.EqualTo(active));

            // Update không làm đổi tổng số bản ghi
            Assert.That(before, Is.EqualTo(_svc.GetAll().Count));
        }

        [Test]
        public void Update_ExistingUser_Succeeds_And_ChangesAllFields()
        {
            const string id = "u100";
            _svc.DelById(id); // reset
            var created = _svc.Create(new UsrEtt
            {
                Id = id, FullName = "Old Name", Address = "Old Addr",
                BirthDay = new DateTime(1999, 9, 9), Description = "Old Desc", Active = false
            });
            Assert.That(string.IsNullOrEmpty(created), Is.True);

            var msg = _svc.Update(new UsrEtt
            {
                Id = id,
                FullName = "New Name",
                Address = "New Addr",
                BirthDay = new DateTime(2000, 1, 1),
                Description = "New Desc",
                Active = true
            });
            Assert.That(string.IsNullOrEmpty(msg), Is.True);

            var found = _svc.GetById(id);
            Assert.That(found, Is.Not.Null);
            Assert.That(found!.FullName, Is.EqualTo("New Name"));
            Assert.That(found.Address, Is.EqualTo("New Addr"));
            Assert.That(found.BirthDay, Is.EqualTo(new DateTime(2000, 1, 1)));
            Assert.That(found.Description, Is.EqualTo("New Desc"));
            Assert.That(found.Active, Is.True);
        }

        [Test]
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

            Assert.That(string.IsNullOrEmpty(msg), Is.False, "Should return error when updating non-existing user.");
            Assert.That(_svc.GetById(id), Is.Null, "Service must not create new user on update.");
            Assert.That(before, Is.EqualTo(_svc.GetAll().Count), "Total count must not change.");
        }

        public interface ITimeProvider { DateTime Now { get; } }
        public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
    }
}