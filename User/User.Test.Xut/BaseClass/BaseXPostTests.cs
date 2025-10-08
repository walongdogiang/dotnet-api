using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Xut.BaseClass
{
    public abstract class BaseXPostTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void Create(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            var countBefore = _svc.GetAll().Count;

            var msg = _svc.Create(new Usr { Id = id, FullName = fullName, Address = address, BirthDay = DateTime.Parse(birthDay), Description = description, Active = active });
            var countAfter = _svc.GetAll().Count;
            Assert.Null(msg);

            var found = _svc.GetById(id);
            Assert.NotNull(found);
            Assert.Equal(id, found.Id);
            Assert.Equal(fullName, found.FullName);
            Assert.Equal(active, found.Active);

            Assert.Equal(countBefore + 1, countAfter);
            Console.WriteLine($"Test Create: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
        }

        public void Create_Duplicate()
        {
            var id = "dup01";
            var newUsr = new Usr { Id = id, FullName = "Vu Hieu 1", Active = true };
            var msg = _svc.Create(newUsr);
            Assert.Null(msg);

            var count = _svc.GetAll().Count;
            var msg2 = _svc.Create(new Usr { Id = id, FullName = "Vu Hieu 2", Active = false });

            Assert.NotNull(msg2);
            Assert.Equal(count, _svc.GetAll().Count);
        }
    }
}
