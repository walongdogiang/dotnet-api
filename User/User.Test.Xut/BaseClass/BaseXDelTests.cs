using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Xut.BaseClass
{
    public abstract class BaseXDelTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void Delete(string id)
        {
            var newUser = new Usr { Id = id, FullName = "Temp", Active = true };
            Assert.Null(_svc.Create(newUser));
            var countBefore = _svc.GetAll().Count;
            var msg = _svc.DelById(id);

            // Assert
            Assert.Null(msg);
            Assert.Equal(countBefore - 1, _svc.GetAll().Count);
            Assert.Null(_svc.GetById(id));
        }

        public void Delete_NotFound()
        {
            var before = _svc.GetAll().Count;

            var msg = _svc.DelById("abc100");

            Assert.NotNull(msg);
            Assert.Equal(before, _svc.GetAll().Count);
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
            Assert.NotNull(list);
            Assert.True(list.Count >= 3);

            foreach (var u in list.ToArray())
            {
                var del = _svc.DelById(u.Id);
                Assert.Null(del);
            }

            // Assert
            Assert.Equal(0, _svc.GetAll().Count);
        }
    }
}
