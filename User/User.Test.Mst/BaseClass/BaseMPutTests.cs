using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Mst.BaseClass
{
    public abstract class BaseMPutTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
    {
        public void Update(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            // Seed nếu chưa có
            if (_svc.GetById(id) == null)
            {
                var seedMsg = _svc.Create(new Usr
                {
                    Id = id, FullName = "Seed Name", Address = "Seed Addr",
                    BirthDay = new DateTime(1980, 1, 1), Description = "Seed Desc", Active = !active
                });
                Assert.IsTrue(string.IsNullOrEmpty(seedMsg), $"Seeding '{id}' should succeed.");
            }

            var before = _svc.GetAll().Count;

            // Act
            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = fullName,
                Address = address,
                BirthDay = DateTime.Parse(birthDay),
                Description = description,
                Active = active
            });

            // Assert
            Assert.IsTrue(string.IsNullOrEmpty(msg), "Update should return null on success.");

            var found = _svc.GetById(id);
            Assert.IsNotNull(found, "User should exist after update.");

            Assert.AreEqual(id, found!.Id);
            Assert.AreEqual(fullName, found.FullName);
            Assert.AreEqual(address, found.Address);
            Assert.AreEqual(DateTime.Parse(birthDay), found.BirthDay);
            Assert.AreEqual(description, found.Description);
            Assert.AreEqual(active, found.Active);

            // Update không làm đổi tổng số bản ghi
            Assert.AreEqual(before, _svc.GetAll().Count);
        }

        public void Update_ExistingUser_Succeeds_And_ChangesAllFields()
        {
            const string id = "u100";
            _svc.DelById(id); // reset
            var created = _svc.Create(new Usr
            {
                Id = id, FullName = "Old Name", Address = "Old Addr",
                BirthDay = new DateTime(1999, 9, 9), Description = "Old Desc", Active = false
            });
            Assert.IsTrue(string.IsNullOrEmpty(created));

            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = "New Name",
                Address = "New Addr",
                BirthDay = new DateTime(2000, 1, 1),
                Description = "New Desc",
                Active = true
            });
            Assert.IsTrue(string.IsNullOrEmpty(msg));

            var found = _svc.GetById(id);
            Assert.IsNotNull(found);
            Assert.AreEqual("New Name", found!.FullName);
            Assert.AreEqual("New Addr", found.Address);
            Assert.AreEqual(new DateTime(2000, 1, 1), found.BirthDay);
            Assert.AreEqual("New Desc", found.Description);
            Assert.IsTrue(found.Active);
        }

        public void Update_NotFound_ReturnsError_And_DoesNotCreate()
        {
            const string id = "u404";
            _svc.DelById(id);
            var before = _svc.GetAll().Count;

            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = "Ghost",
                Address = "No Addr",
                BirthDay = new DateTime(1970, 1, 1),
                Description = "Should fail",
                Active = false
            });

            Assert.IsFalse(string.IsNullOrEmpty(msg), "Should return error when updating non-existing user.");
            Assert.IsNull(_svc.GetById(id), "Service must not create new user on update.");
            Assert.AreEqual(before, _svc.GetAll().Count, "Total count must not change.");
        }
    }
}
