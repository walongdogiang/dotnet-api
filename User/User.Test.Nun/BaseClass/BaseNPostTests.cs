using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Nun.BaseClass
{
    public abstract class BaseNPostTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void Create(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            var countBefore = _svc.GetAll().Count;

            var msg = _svc.Create(new Usr { Id = id, FullName = fullName, Address = address, BirthDay = DateTime.Parse(birthDay), Description = description, Active = active });
            var countAfter = _svc.GetAll().Count;
            Assert.That(msg, Is.Null, "Create should return null on success.");

            var found = _svc.GetById(id);
            Assert.That(found, Is.Not.Null);
            Assert.That(found.Id, Is.EqualTo(id));
            Assert.That(found.FullName, Is.EqualTo(fullName));
            Assert.That(found.Active, Is.EqualTo(active));

            Assert.That(countAfter, Is.EqualTo(countBefore + 1));
            Console.WriteLine($"Test Create: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
        }

        public void Create_Duplicate()
        {
            var id = "dup01";
            var newUsr = new Usr { Id = id, FullName = "Vu Hieu 1", Active = true };
            var msg = _svc.Create(newUsr);
            Assert.That(msg, Is.Null, "First create should succeed.");

            var count = _svc.GetAll().Count;
            var msg2 = _svc.Create(new Usr { Id = id, FullName = "Vu Hieu 2", Active = false });

            Assert.That(msg2, Is.Not.Null, "Second create with duplicate ID should return error message.");
            Assert.That(_svc.GetAll().Count, Is.EqualTo(count));
        }
    }
}
