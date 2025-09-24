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
    public sealed class NPostTests
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

        [TestCase("p01", "Nguyen Van A", "123 Street, City", "1990-01-01", "Description for Nguyen Van A", true)]
        [TestCase("p02", "Tran Thi B", "456 Avenue, City", "1992-02-02", "Description for Tran Thi B", false)]
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

            Assert.That(msg, Is.Null, "Create should return null on success.");

            var found = _svc.GetById(id);
            Assert.That(found, Is.Not.Null);
            Assert.That(found!.Id, Is.EqualTo(id));
            Assert.That(found.FullName, Is.EqualTo(fullName));
            Assert.That(found.Active, Is.EqualTo(active));

            Assert.That(countAfter, Is.EqualTo(countBefore + 1));
            TestContext.Out.WriteLine($"Created: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
        }

        [Test]
        public void Create_DuplicateId_ReturnsError_And_NotChangeCount()
        {
            var id = "dup01";
            var newUsr = new UsrEtt { Id = id, FullName = "Vu Hieu 1", Active = true };
            var msg = _svc.Create(newUsr);
            Assert.That(msg, Is.Null, "First create should succeed.");

            var count = _svc.GetAll().Count;
            var msg2 = _svc.Create(new UsrEtt { Id = id, FullName = "Vu Hieu 2", Active = false });

            Assert.That(msg2, Is.Not.Null, "Second create with duplicate ID should return error message.");
            Assert.That(_svc.GetAll().Count, Is.EqualTo(count), "User count should not change.");
        }

        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider
        {
            public DateTime Now => DateTime.Now;
        }
    }
}