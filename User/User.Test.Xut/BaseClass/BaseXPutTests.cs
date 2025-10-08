using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;

namespace User.Test.Xut.BaseClass
{
    public abstract class BaseXPutTests<TSvc> : BaseUserTests<TSvc> where TSvc : class, IUsersSvc
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
                Assert.True(string.IsNullOrEmpty(seedMsg));
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
            Assert.True(string.IsNullOrEmpty(msg));

            var found = _svc.GetById(id);
            Assert.NotNull(found);

            Assert.Equal(id, found!.Id);
            Assert.Equal(fullName, found.FullName);
            Assert.Equal(address, found.Address);
            Assert.Equal(DateTime.Parse(birthDay), found.BirthDay);
            Assert.Equal(description, found.Description);
            Assert.Equal(active, found.Active);

            // Update không làm đổi tổng số bản ghi
            Assert.Equal(before, _svc.GetAll().Count);
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
            Assert.True(string.IsNullOrEmpty(created));

            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = "New Name",
                Address = "New Addr",
                BirthDay = new DateTime(2000, 1, 1),
                Description = "New Desc",
                Active = true
            });
            Assert.True(string.IsNullOrEmpty(msg));

            var found = _svc.GetById(id);
            Assert.NotNull(found);
            Assert.Equal("New Name", found!.FullName);
            Assert.Equal("New Addr", found.Address);
            Assert.Equal(new DateTime(2000, 1, 1), found.BirthDay);
            Assert.Equal("New Desc", found.Description);
            Assert.True(found.Active);
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

            Assert.False(string.IsNullOrEmpty(msg));
            Assert.Null(_svc.GetById(id));
            Assert.Equal(before, _svc.GetAll().Count);
        }
    }
}
