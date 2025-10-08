using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Mst.BaseClass
{
    public abstract class BaseMGetTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void GetById(string id, string fullName, bool active)
        {
            Console.WriteLine($"TestGetById executed at: {DateTime.Now}");

            var found = _svc.GetById(id);

            Assert.IsNotNull(found, $"User with ID '{id}' should exist.");
            Assert.AreEqual(id, found.Id, $"Id should match with '{id}'.");
            Assert.AreEqual(fullName, found.FullName, $"Fullname should match with '{fullName}'.");
            Assert.AreEqual(active, found.Active, $"Active status should match with '{active}'.");

            Console.WriteLine($"Info: Id={found.Id} | Fullname={found.FullName} | active={found.Active}");
        }

        public void GetAllUser()
        {
            var countUsers = _svc.GetAll().Count;
            _svc.Create(new Usr { Id = "10", FullName = "Jully", Active = true });
            _svc.Create(new Usr { Id = "11", FullName = "October", Active = false });

            Console.WriteLine("TestGetAllUser");

            var users = _svc.GetAll();
            Assert.IsNotNull(users);
            Assert.AreEqual(countUsers + 2, users.Count, "User count should increase by 2 after adding 2 users.");

            Console.WriteLine($"[COUNT] users = {users.Count}");
        }

        public void GetByKwd(string keyword, int expectedCount)
        {
            var users = _svc.GetByKwd(keyword);
            Assert.IsNotNull(users);
            Assert.AreEqual(expectedCount, users.Count, $"Should find {expectedCount} users with keyword '{keyword}'");
            Console.WriteLine($"Test GetByKwd: keyword='{keyword}' | Found {users.Count} users");
        }
    }
}
