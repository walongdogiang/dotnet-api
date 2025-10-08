using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using User.Svc;

namespace User.Test.Mst.Cases
{
    [TestClass]
    public sealed class MPostTests
    {
        private IUsersSvc _svc;

        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection()
                .AddSingleton<ITimeProvider, SystemTimeProvider>()
                .AddSingleton<IUsersSvc, UsersSvc>();

            var provider = services.BuildServiceProvider();
            _svc = provider.GetRequiredService<IUsersSvc>();
        }

        [DataTestMethod]
        [DataRow("p01", "Nguyen Van A", "123 Street, City", "1990-01-01", "Description for Nguyen Van A", true)]
        [DataRow("p02", "Tran Thi B", "456 Avenue, City", "1992-02-02", "Description for Tran Thi B", false)]
        public void TestCreate(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            var countBefore = _svc.GetAll().Count;

            var msg = _svc.Create(new Usr { Id = id, FullName = fullName, Address = address, BirthDay = DateTime.Parse(birthDay), Description = description, Active = active });
            var countAfter = _svc.GetAll().Count;
            Assert.IsNull(msg, "Create should return null on success.");

            var found = _svc.GetById(id);
            Assert.IsNotNull(found);
            Assert.AreEqual(id, found.Id);
            Assert.AreEqual(fullName, found.FullName);
            Assert.AreEqual(active, found.Active);

            Assert.AreEqual(countBefore + 1, countAfter);
            Console.WriteLine($"Test Create: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
        }

        // Trùng ID phải lỗi và không đổi tổng số user
        [TestMethod]
        public void TestCreate_Duplicate()
        {
            var id = "dup01";
            var newUsr = new Usr { Id = id, FullName = "Vu Hieu 1", Active = true };
            var msg = _svc.Create(newUsr);
            Assert.IsNull(msg, "First create should succeed.");

            var count = _svc.GetAll().Count;
            var msg2 = _svc.Create(new Usr { Id = id, FullName = "Vu Hieu 2", Active = false });

            Assert.IsNotNull(msg2, "Second create with duplicate ID should return error message.");
            Assert.AreEqual(count, _svc.GetAll().Count);
        }
        
        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider
        {
            public DateTime Now => DateTime.Now;
        }
    }
}