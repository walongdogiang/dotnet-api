using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.Models;
using User.Db.MSSQL.EF.Entity;
using User.Svc;

namespace User.Test.Mst.Db.MSSQL.EF.Cases
{
    [TestClass]
    public sealed class MDelTests
    {
        private IUsersSvc _svc;

        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();

            // DbContext InMemory: mỗi test class 1 DB riêng bằng Guid
            services.AddDbContext<UsrDbContext>(opt =>
                opt.UseInMemoryDatabase($"mst-ef-{Guid.NewGuid()}"));

            // Đăng ký EFUsrsSvc thay vì UsersSvc
            services.AddSingleton<ITimeProvider, SystemTimeProvider>().AddSingleton<IUsersSvc, EFUsrsSvc>();

            var provider = services.BuildServiceProvider();
            _svc = provider.GetRequiredService<IUsersSvc>();
        }

        // Test method với DataTestMethod & DataRow
        [DataTestMethod]
        [DataRow("d01")]
        [DataRow("d02")]
        public void TestDelete(string id)
        {
            var newUser = new Usr { Id = id, FullName = "Temp", Active = true };
            Assert.IsNull(_svc.Create(newUser), "New user should be created without error.");
            var countBefore = _svc.GetAll().Count;
            var msg = _svc.DelById(id);

            // Assert
            Assert.IsNull(msg, "Delete existing should return null.");
            Assert.AreEqual(countBefore - 1, _svc.GetAll().Count, "User count should decrease by 1.");
            Assert.IsNull(_svc.GetById(id), "Deleted user must not be found.");
        }

        // Test delete user không tồn tại
        [TestMethod]
        public void TestDelete_NotFound()
        {
            var before = _svc.GetAll().Count;

            var msg = _svc.DelById("abc100");

            Assert.IsNotNull(msg, "Deleting non-existing should return error message.");
            Assert.AreEqual(before, _svc.GetAll().Count, "Count must not change when deleting non-existing user.");
        }

        // Test delete tất cả users (duyệt và xóa từng user)
        [TestMethod]
        public void TestDeleteAll()
        {
            // Arrange: seed vài user
            var count = _svc.GetAll().Count;
            if (count == 0)
            {
                _svc.Create(new Usr { Id = "a1", FullName = "A", Active = true });
                _svc.Create(new Usr { Id = "a2", FullName = "B", Active = true });
                _svc.Create(new Usr { Id = "a3", FullName = "C", Active = true });
            }

            var list = _svc.GetAll();
            Assert.IsNotNull(list);
            Assert.IsTrue(list.Count >= 3, "Seed should create at least 3 users.");

            foreach (var u in list.ToArray())
            {
                var del = _svc.DelById(u.Id);
                Assert.IsNull(del, $"Delete [{u.Id} - {u.FullName}] should succeed.");
            }

            // Assert
            Assert.AreEqual(0, _svc.GetAll().Count, "All users should be deleted.");
        }
        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider
        {
            public DateTime Now => DateTime.Now;
        }
    }
}