using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Nun.BaseClass
{
    public abstract class BaseNDelTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void Delete(string id)
        {
            var newUser = new Usr { Id = id, FullName = "Temp", Active = true };
            Assert.That(_svc.Create(newUser), Is.Null, "New user should be created without error.");
            var countBefore = _svc.GetAll().Count;
            var msg = _svc.DelById(id);

            // Assert
            Assert.That(msg, Is.Null, "Delete existing should return null.");
            Assert.That(_svc.GetAll().Count, Is.EqualTo(countBefore - 1), "User count should decrease by 1.");
            Assert.That(_svc.GetById(id), Is.Null, "Deleted user must not be found.");
        }

        public void Delete_NotFound()
        {
            var before = _svc.GetAll().Count;

            var msg = _svc.DelById("abc100");

            Assert.That(msg, Is.Not.Null, "Deleting non-existing should return error message.");
            Assert.That(_svc.GetAll().Count, Is.EqualTo(before), "Count must not change when deleting non-existing user.");
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
            Assert.That(list, Is.Not.Null);
            Assert.That(list.Count, Is.GreaterThanOrEqualTo(3), "Seed should create at least 3 users.");

            foreach (var u in list.ToArray())
            {
                var del = _svc.DelById(u.Id);
                Assert.That(del, Is.Null, $"Delete [{u.Id} - {u.FullName}] should succeed.");
            }

            // Assert
            Assert.That(_svc.GetAll().Count, Is.EqualTo(0), "All users should be deleted.");
        }
    }
}
