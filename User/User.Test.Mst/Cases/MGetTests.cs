using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.Models;
using User.Db.MSSQL.EF.Entity;

namespace User.Test.Mst.Db.MSSQL.EF.Cases
{
    [TestClass]
    public sealed class MGetTests
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

        [DataTestMethod]
        [DataRow("1", "Nguyen Van A", true)]
        [DataRow("2", "Tran Thi B", false)]
        [DataRow("3", "Le Van C", true)]
        public void TestGetById(string id, string fullName, bool active)
        {
            Console.WriteLine($"TestGetById executed at: {DateTime.Now}");

            var found = _svc.GetById(id);

            Assert.IsNotNull(found, $"User with ID '{id}' should exist.");
            Assert.AreEqual(id, found.Id, $"Id should match with '{id}'.");
            Assert.AreEqual(fullName, found.FullName, $"Fullname should match with '{fullName}'.");
            Assert.AreEqual(active, found.Active, $"Active status should match with '{active}'.");

            Console.WriteLine($"Info: Id={found.Id} | Fullname={found.FullName} | active={found.Active}");
        }

        [TestMethod]
        public void TestGetAllUser()
        {
            var countUsers = _svc.GetAll().Count;
            _svc.Create(new UsrEtt { Id = "10", FullName = "Jully", Active = true });
            _svc.Create(new UsrEtt { Id = "11", FullName = "October", Active = false });

            Console.WriteLine("TestGetAllUser");

            var users = _svc.GetAll();
            Assert.IsNotNull(users);
            Assert.AreEqual(countUsers + 2, users.Count, "User count should increase by 2 after adding 2 users.");

            Console.WriteLine($"[COUNT] users = {users.Count}");
        }
        
        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider
        {
            public DateTime Now => DateTime.Now;
        }
    }
}
