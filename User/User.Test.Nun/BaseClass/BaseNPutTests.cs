using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Nun.BaseClass
{
    public abstract class BaseNPutTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
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
                Assert.That(string.IsNullOrEmpty(seedMsg), Is.True, $"Seeding '{id}' should succeed.");
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
            Assert.That(string.IsNullOrEmpty(msg), Is.True, "Update should return null on success.");

            var found = _svc.GetById(id);
            Assert.That(found, Is.Not.Null, "User should exist after update.");

            Assert.That(found!.Id, Is.EqualTo(id));
            Assert.That(found.FullName, Is.EqualTo(fullName));
            Assert.That(found.Address, Is.EqualTo(address));
            Assert.That(found.BirthDay, Is.EqualTo(DateTime.Parse(birthDay)));
            Assert.That(found.Description, Is.EqualTo(description));
            Assert.That(found.Active, Is.EqualTo(active));

            // Update không làm đổi tổng số bản ghi
            Assert.That(_svc.GetAll().Count, Is.EqualTo(before));
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
            Assert.That(string.IsNullOrEmpty(created), Is.True);

            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = "New Name",
                Address = "New Addr",
                BirthDay = new DateTime(2000, 1, 1),
                Description = "New Desc",
                Active = true
            });
            Assert.That(string.IsNullOrEmpty(msg), Is.True);

            var found = _svc.GetById(id);
            Assert.That(found, Is.Not.Null);
            Assert.That(found!.FullName, Is.EqualTo("New Name"));
            Assert.That(found.Address, Is.EqualTo("New Addr"));
            Assert.That(found.BirthDay, Is.EqualTo(new DateTime(2000, 1, 1)));
            Assert.That(found.Description, Is.EqualTo("New Desc"));
            Assert.That(found.Active, Is.True);
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

            Assert.That(string.IsNullOrEmpty(msg), Is.False, "Should return error when updating non-existing user.");
            Assert.That(_svc.GetById(id), Is.Null, "Service must not create new user on update.");
            Assert.That(_svc.GetAll().Count, Is.EqualTo(before), "Total count must not change.");
        }
    }
}
