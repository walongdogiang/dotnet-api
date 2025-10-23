using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using User.Svc;
using User.Svc.Cache;
using User.Test.Mst.BaseClass;

namespace User.Test.Mst.Cache.Redis
{
    [TestClass]
    public sealed class MPutTests : BaseRedisTests<RdsUsrSvc>
    {
        [TestInitialize]
        public new void Setup()
        {
            base.Setup();
            ClearCache(); // Clear cache trước mỗi test
        }

        [DataTestMethod]
        [DataRow("ru01", "Redis Update User 1", "1 Redis ABC Street", "1990-01-01", "First Redis update", true)]
        [DataRow("ru02", "Redis Update User 2", "2 Redis DEF Avenue", "1992-02-02", "Second Redis update", false)]
        [DataRow("ru03", "Redis Update User 3", "3 Redis GHI Road", "1995-03-03", "Third Redis update", true)]
        public void TestUpdate_AllFields_WithCache(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            // Arrange - Tạo user trước
            var seedUser = new Usr
            {
                Id = id,
                FullName = "Seed Name",
                Address = "Seed Address",
                BirthDay = new DateTime(1980, 1, 1),
                Description = "Seed Description",
                Active = !active
            };

            var seedMsg = _svc.Create(seedUser);
            Assert.IsTrue(string.IsNullOrEmpty(seedMsg), $"Seeding '{id}' should succeed.");

            var before = _fallbackService.GetAll().Count;

            // Act - Update user
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

            // Verify trong fallback service
            var found = _fallbackService.GetById(id);
            Assert.IsNotNull(found, "User should exist after update.");

            Assert.AreEqual(id, found.Id);
            Assert.AreEqual(fullName, found.FullName);
            Assert.AreEqual(address, found.Address);
            Assert.AreEqual(DateTime.Parse(birthDay), found.BirthDay);
            Assert.AreEqual(description, found.Description);
            Assert.AreEqual(active, found.Active);

            // Update không làm đổi tổng số bản ghi
            Assert.AreEqual(before, _fallbackService.GetAll().Count);

            // Verify cache invalidation
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheMiss(userCacheKey); // User cache should be invalidated
            VerifyCacheMiss("users:all"); // All users cache should be invalidated

            Console.WriteLine($"Test Update with Cache: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
        }

        [TestMethod]
        public void TestUpdate_ExistingUser_Succeeds_And_ChangesAllFields_WithCache()
        {
            // Arrange
            const string id = "ru100";
            _svc.DelById(id); // reset

            var created = _svc.Create(new Usr
            {
                Id = id,
                FullName = "Old Redis Name",
                Address = "Old Redis Address",
                BirthDay = new DateTime(1999, 9, 9),
                Description = "Old Redis Description",
                Active = false
            });
            Assert.IsTrue(string.IsNullOrEmpty(created));

            // Act
            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = "New Redis Name",
                Address = "New Redis Address",
                BirthDay = new DateTime(2000, 1, 1),
                Description = "New Redis Description",
                Active = true
            });

            // Assert
            Assert.IsTrue(string.IsNullOrEmpty(msg));

            var found = _fallbackService.GetById(id);
            Assert.IsNotNull(found);
            Assert.AreEqual("New Redis Name", found.FullName);
            Assert.AreEqual("New Redis Address", found.Address);
            Assert.AreEqual(new DateTime(2000, 1, 1), found.BirthDay);
            Assert.AreEqual("New Redis Description", found.Description);
            Assert.IsTrue(found.Active);

            // Verify cache behavior
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheMiss(userCacheKey);
            VerifyCacheMiss("users:all");
        }

        [TestMethod]
        public void TestUpdate_NotFound_ReturnsError_And_DoesNotCreate_WithCache()
        {
            // Arrange
            const string id = "ru404";
            _svc.DelById(id);
            var before = _fallbackService.GetAll().Count;

            // Act
            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = "Ghost Redis User",
                Address = "No Redis Address",
                BirthDay = new DateTime(1970, 1, 1),
                Description = "Should fail",
                Active = false
            });

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(msg), "Should return error when updating non-existing user.");
            Assert.IsNull(_fallbackService.GetById(id), "Service must not create new user on update.");
            Assert.AreEqual(before, _fallbackService.GetAll().Count, "Total count must not change.");

            // Verify no cache operations occurred
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheMiss(userCacheKey);
        }

        [TestMethod]
        public void TestUpdate_InvalidUser_ShouldReturnError()
        {
            // Test với user null
            var msg1 = _svc.Update(null);
            Assert.IsNotNull(msg1, "Updating null user should return error");

            // Test với ID null
            var msg2 = _svc.Update(new Usr { Id = null, FullName = "Test", Active = true });
            Assert.IsNotNull(msg2, "Updating user with null ID should return error");

            // Test với ID empty
            var msg3 = _svc.Update(new Usr { Id = "", FullName = "Test", Active = true });
            Assert.IsNotNull(msg3, "Updating user with empty ID should return error");
        }

        [TestMethod]
        public void TestUpdate_CacheInvalidation_Behavior()
        {
            // Arrange
            const string id = "cache_update_test";
            var user = new Usr { Id = id, FullName = "Cache Update Test", Active = true };
            _svc.Create(user);

            // Cache user data
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheHit(userCacheKey);

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("users:all");

            // Act - Update user
            var updatedUser = new Usr { Id = id, FullName = "Updated Cache Test", Active = false };
            var msg = _svc.Update(updatedUser);

            // Assert
            Assert.IsNull(msg, "Update should succeed");
            VerifyCacheMiss(userCacheKey); // User cache should be invalidated
            VerifyCacheMiss("users:all"); // All users cache should be invalidated

            // Verify cache gets rebuilt
            var updatedCachedUser = _svc.GetById(id);
            Assert.IsNotNull(updatedCachedUser);
            Assert.AreEqual("Updated Cache Test", updatedCachedUser.FullName);
            Assert.IsFalse(updatedCachedUser.Active);
            VerifyCacheHit(userCacheKey); // Cache should be rebuilt

            var users2 = _svc.GetAll();
            VerifyCacheHit("users:all"); // Cache should be rebuilt
        }

        [TestMethod]
        public void TestUpdate_CacheConsistency()
        {
            // Arrange
            const string id = "consistency_test";
            var user = new Usr { Id = id, FullName = "Consistency Test", Active = true };
            _svc.Create(user);

            // Act - Multiple updates
            for (int i = 1; i <= 3; i++)
            {
                var updateUser = new Usr 
                { 
                    Id = id, 
                    FullName = $"Consistency Test {i}", 
                    Active = i % 2 == 0 
                };
                
                var msg = _svc.Update(updateUser);
                Assert.IsNull(msg, $"Update {i} should succeed");

                // Verify data consistency
                var found = _fallbackService.GetById(id);
                Assert.IsNotNull(found);
                Assert.AreEqual($"Consistency Test {i}", found.FullName);
                Assert.AreEqual(i % 2 == 0, found.Active);

                // Verify cache is invalidated after each update
                var userCacheKey = string.Format("user:id:{0}", id);
                VerifyCacheMiss(userCacheKey);
                VerifyCacheMiss("users:all");
            }
        }
    }
}
