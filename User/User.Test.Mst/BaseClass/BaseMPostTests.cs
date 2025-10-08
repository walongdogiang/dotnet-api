using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Mst.BaseClass
{
    public abstract class BaseMPostTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void Create(string id, string fullName, string address, string birthDay, string description, bool active)
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

        public void Create_Duplicate()
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
    }
}
