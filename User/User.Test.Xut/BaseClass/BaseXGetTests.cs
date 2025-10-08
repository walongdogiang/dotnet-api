using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Xut.BaseClass
{
    public abstract class BaseXGetTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void GetById(string id, string fullName, bool active)
        {
            Console.WriteLine($"TestGetById executed at: {DateTime.Now}");

            var found = _svc.GetById(id);

            Assert.NotNull(found);
            Assert.Equal(id, found.Id);
            Assert.Equal(fullName, found.FullName);
            Assert.Equal(active, found.Active);

            Console.WriteLine($"Info: Id={found.Id} | Fullname={found.FullName} | active={found.Active}");
        }

        public void GetAllUser()
        {
            var countUsers = _svc.GetAll().Count;
            _svc.Create(new Usr { Id = "10", FullName = "Jully", Active = true });
            _svc.Create(new Usr { Id = "11", FullName = "October", Active = false });

            Console.WriteLine("TestGetAllUser");

            var users = _svc.GetAll();
            Assert.NotNull(users);
            Assert.Equal(countUsers + 2, users.Count);

            Console.WriteLine($"[COUNT] users = {users.Count}");
        }

        public void GetByKwd(string keyword, int expectedCount)
        {
            var users = _svc.GetByKwd(keyword);
            Assert.NotNull(users);
            Assert.Equal(expectedCount, users.Count);
            Console.WriteLine($"Test GetByKwd: keyword='{keyword}' | Found {users.Count} users");
        }
    }
}
