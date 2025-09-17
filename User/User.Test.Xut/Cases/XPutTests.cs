using Xunit;
using User.Svc;
using Microsoft.Extensions.DependencyInjection;

namespace User.Test.Xut.Cases
{
    /// <summary>
    /// Unit tests for UPDATE (PUT) scenarios:
    /// - Parameterized update (verify all fields)
    /// - Update existing user
    /// - Update non-existing user
    /// Assumes UsersSvc.Update(Usr) returns null on success, error message otherwise.
    /// </summary>
    public sealed class XPutTests
    {
        private readonly IUsersSvc _svc;

        public XPutTests()
        {
            // DI giống các test khác để đồng bộ
            var services = new ServiceCollection()
                .AddSingleton<ITimeProvider, SystemTimeProvider>()
                .AddSingleton<IUsersSvc, UsersSvc>();

            _svc = services.BuildServiceProvider().GetRequiredService<IUsersSvc>();
        }

        // ====== 1) Test method tham số hóa (Parameterized) ======
        [Theory]
        [InlineData("u01", "Vu Hieu",   "1 ABC Street", "1990-01-01", "First update",  true)]
        [InlineData("u02", "Hong Tam",  "2 DEF Avenue", "1992-02-02", "Second update", false)]
        [InlineData("u03", "5ilence",   "3 GHI Road",   "1995-03-03", "Third update",  true)]
        public void TestUpdate_AllFields_Verified(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            // Seed: tạo user ban đầu nếu chưa có
            if (_svc.GetById(id) == null)
            {
                var seedMsg = _svc.Create(new Usr
                {
                    Id = id, FullName = "Seed Name", Address = "Seed Addr",
                    BirthDay = new DateTime(1980, 1, 1), Description = "Seed Desc", Active = !active
                });
                Assert.True(string.IsNullOrEmpty(seedMsg), $"Seeding user '{id}' should succeed.");
            }

            var before = _svc.GetAll().Count;

            // Act: UPDATE tất cả trường
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
            Assert.True(string.IsNullOrEmpty(msg), "Update should return null on success.");

            var found = _svc.GetById(id);
            Assert.NotNull(found);

            Assert.Equal(id,          found!.Id);
            Assert.Equal(fullName,    found.FullName);
            Assert.Equal(address,     found.Address);
            Assert.Equal(DateTime.Parse(birthDay), found.BirthDay);
            Assert.Equal(description, found.Description);
            Assert.Equal(active,      found.Active);

            // Không thay đổi tổng số bản ghi
            Assert.Equal(before, _svc.GetAll().Count);
        }

        // ====== 2) Update user tồn tại (trường hợp riêng, dễ đọc log) ======
        [Fact]
        public void Update_ExistingUser_Succeeds_And_ChangesAllFields()
        {
            var id = "u100";
            // Create or reset
            _svc.DelById(id);
            Assert.True(string.IsNullOrEmpty(_svc.Create(new Usr
            {
                Id = id, FullName = "Old Name", Address = "Old Addr",
                BirthDay = new DateTime(1999, 9, 9), Description = "Old Desc", Active = false
            })));

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

        // ====== 3) Update user không tồn tại ======
        [Fact]
        public void Update_NotFound_ReturnsError_And_DoesNotCreate()
        {
            var id = "u404";
            // Ensure not exists
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

            Assert.False(string.IsNullOrEmpty(msg));      // phải có lỗi
            Assert.Null(_svc.GetById(id));                // không tự tạo mới
            Assert.Equal(before, _svc.GetAll().Count);    // không đổi số lượng
        }

        public interface ITimeProvider { DateTime Now { get; } }
        public sealed class SystemTimeProvider : ITimeProvider { public DateTime Now => DateTime.Now; }
    }
}
