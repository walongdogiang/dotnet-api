using System;
using System.Linq;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.Models;
using User.Db.MSSQL.EF.Entity;

namespace User.Test.Nun.Db.MSSQL.EF.Cases
{
    [TestFixture]
    public sealed class NDelTests
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

        [TestCase("d01")]
        [TestCase("d02")]
        public void Delete_ExistingUser_Succeeds(string id)
        {
            var newUser = new UsrEtt { Id = id, FullName = "Temp", Active = true };
            Assert.That(_svc.Create(newUser), Is.Null, "New user should be created without error.");
            var countBefore = _svc.GetAll().Count;

            var msg = _svc.DelById(id);

            Assert.That(msg, Is.Null, "Delete existing should return null.");
            Assert.That(_svc.GetAll().Count, Is.EqualTo(countBefore - 1), "User count should decrease by 1.");
            Assert.That(_svc.GetById(id), Is.Null, "Deleted user must not be found.");
        }

        [Test]
        public void Delete_NotFound_ReturnsError_And_KeepCount()
        {
            var before = _svc.GetAll().Count;

            var msg = _svc.DelById("abc100");

            Assert.That(msg, Is.Not.Null, "Deleting non-existing should return error message.");
            Assert.That(_svc.GetAll().Count, Is.EqualTo(before), "Count must not change when deleting non-existing user.");
        }

        [Test]
        public void Delete_AllUsers_EmptiesCollection()
        {
            // Arrange: seed vài user nếu rỗng
            if (_svc.GetAll().Count == 0)
            {
                _svc.Create(new UsrEtt { Id = "a1", FullName = "A", Active = true });
                _svc.Create(new UsrEtt { Id = "a2", FullName = "B", Active = true });
                _svc.Create(new UsrEtt { Id = "a3", FullName = "C", Active = true });
            }

            var list = _svc.GetAll();
            Assert.That(list, Is.Not.Null);
            Assert.That(list.Count, Is.GreaterThanOrEqualTo(3), "Seed should create at least 3 users.");

            foreach (var u in list.ToArray())
            {
                var del = _svc.DelById(u.Id);
                Assert.That(del, Is.Null, $"Delete [{u.Id} - {u.FullName}] should succeed.");
            }

            Assert.That(_svc.GetAll().Count, Is.EqualTo(0), "All users should be deleted.");
        }

        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider
        {
            public DateTime Now => DateTime.Now;
        }
    }
}