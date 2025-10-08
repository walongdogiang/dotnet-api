using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Mst.BaseClass
{
    public abstract class BaseMDelTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void Delete(string id)
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

        public void Delete_NotFound()
        {
            var before = _svc.GetAll().Count;

            var msg = _svc.DelById("abc100");

            Assert.IsNotNull(msg, "Deleting non-existing should return error message.");
            Assert.AreEqual(before, _svc.GetAll().Count, "Count must not change when deleting non-existing user.");
        }

        public void DeleteAll()
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
    }
}
