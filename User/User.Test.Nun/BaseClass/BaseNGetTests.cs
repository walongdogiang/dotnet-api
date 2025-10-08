using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Nun.BaseClass
{
    public abstract class BaseNGetTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void GetById(string id, string fullName, bool active)
        {
            Console.WriteLine($"TestGetById executed at: {DateTime.Now}");

            var found = _svc.GetById(id);

            Assert.That(found, Is.Not.Null, $"User with ID '{id}' should exist.");
            Assert.That(found.Id, Is.EqualTo(id), $"Id should match with '{id}'.");
            Assert.That(found.FullName, Is.EqualTo(fullName), $"Fullname should match with '{fullName}'.");
            Assert.That(found.Active, Is.EqualTo(active), $"Active status should match with '{active}'.");

            Console.WriteLine($"Info: Id={found.Id} | Fullname={found.FullName} | active={found.Active}");
        }

        public void GetAllUser()
        {
            var countUsers = _svc.GetAll().Count;
            _svc.Create(new Usr { Id = "10", FullName = "Jully", Active = true });
            _svc.Create(new Usr { Id = "11", FullName = "October", Active = false });

            Console.WriteLine("TestGetAllUser");

            var users = _svc.GetAll();
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count, Is.EqualTo(countUsers + 2), "User count should increase by 2 after adding 2 users.");

            Console.WriteLine($"[COUNT] users = {users.Count}");
        }

        public void GetByKwd(string keyword, int expectedCount)
        {
            var users = _svc.GetByKwd(keyword);
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count, Is.EqualTo(expectedCount), $"Should find {expectedCount} users with keyword '{keyword}'");
            Console.WriteLine($"Test GetByKwd: keyword='{keyword}' | Found {users.Count} users");
        }
    }
}
