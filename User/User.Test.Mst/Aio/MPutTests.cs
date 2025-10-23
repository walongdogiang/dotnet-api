using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using User.Svc;
using User.Svc.Cache;
using User.Svc.Aio;
using User.Test.Mst.BaseClass;

namespace User.Test.Mst.Aio
{
    [TestClass]
    public sealed class MPutTests : BaseAioTests<AioUsrSvc>
    {
        [TestInitialize]
        public new void Setup()
        {
            base.Setup();
            ClearCache(); // Clear cache trước mỗi test
        }

        [DataTestMethod]
        [DataRow("au01", "Aio Update User 1", "1 Aio ABC Street", "1990-01-01", "First Aio update", true)]
        [DataRow("au02", "Aio Update User 2", "2 Aio DEF Avenue", "1992-02-02", "Second Aio update", false)]
        [DataRow("au03", "Aio Update User 3", "3 Aio GHI Road", "1995-03-03", "Third Aio update", true)]
        public void TestUpdate_AllFields_WithCacheAndDatabase(string id, string fullName, string address, string birthDay, string description, bool active)
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

            var before = _dbService.GetAll().Count;

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

            // Verify trong database
            var found = _dbService.GetById(id);
            Assert.IsNotNull(found, "User should exist after update.");

            Assert.AreEqual(id, found.Id);
            Assert.AreEqual(fullName, found.FullName);
            Assert.AreEqual(address, found.Address);
            Assert.AreEqual(DateTime.Parse(birthDay), found.BirthDay);
            Assert.AreEqual(description, found.Description);
            Assert.AreEqual(active, found.Active);

            // Update không làm đổi tổng số bản ghi
            Assert.AreEqual(before, _dbService.GetAll().Count);

            // Verify cache invalidation
            VerifyCacheInvalidation(id);

            Console.WriteLine($"Test Update with Cache & DB: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
        }

        [TestMethod]
        public void TestUpdate_ExistingUser_Succeeds_And_ChangesAllFields_WithCache()
        {
            // Arrange
            const string id = "au100";
            _svc.DelById(id); // reset

            var created = _svc.Create(new Usr
            {
                Id = id,
                FullName = "Old Aio Name",
                Address = "Old Aio Address",
                BirthDay = new DateTime(1999, 9, 9),
                Description = "Old Aio Description",
                Active = false
            });
            Assert.IsTrue(string.IsNullOrEmpty(created));

            // Act
            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = "New Aio Name",
                Address = "New Aio Address",
                BirthDay = new DateTime(2000, 1, 1),
                Description = "New Aio Description",
                Active = true
            });

            // Assert
            Assert.IsTrue(string.IsNullOrEmpty(msg));

            var found = _dbService.GetById(id);
            Assert.IsNotNull(found);
            Assert.AreEqual("New Aio Name", found.FullName);
            Assert.AreEqual("New Aio Address", found.Address);
            Assert.AreEqual(new DateTime(2000, 1, 1), found.BirthDay);
            Assert.AreEqual("New Aio Description", found.Description);
            Assert.IsTrue(found.Active);

            // Verify cache behavior
            VerifyCacheInvalidation(id);
        }

        [TestMethod]
        public void TestUpdate_NotFound_ReturnsError_And_DoesNotCreate_WithCache()
        {
            // Arrange
            const string id = "au404";
            _svc.DelById(id);
            var before = _dbService.GetAll().Count;

            // Act
            var msg = _svc.Update(new Usr
            {
                Id = id,
                FullName = "Ghost Aio User",
                Address = "No Aio Address",
                BirthDay = new DateTime(1970, 1, 1),
                Description = "Should fail",
                Active = false
            });

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(msg), "Should return error when updating non-existing user.");
            Assert.IsNull(_dbService.GetById(id), "Service must not create new user on update.");
            Assert.AreEqual(before, _dbService.GetAll().Count, "Total count must not change.");

            // Verify no cache operations occurred
            VerifyCacheMiss($"aio:user:id:{id}");
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
            const string id = "aio_cache_update_test";
            var user = new Usr { Id = id, FullName = "Cache Update Test", Active = true };
            _svc.Create(user);

            // Cache user data
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = $"aio:user:id:{id}";
            VerifyCacheHit(userCacheKey);

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("aio:users:all");

            // Act - Update user
            var updatedUser = new Usr { Id = id, FullName = "Updated Cache Test", Active = false };
            var msg = _svc.Update(updatedUser);

            // Assert
            Assert.IsNull(msg, "Update should succeed");
            VerifyCacheInvalidation(id);

            // Verify cache gets rebuilt
            var updatedCachedUser = _svc.GetById(id);
            Assert.IsNotNull(updatedCachedUser);
            Assert.AreEqual("Updated Cache Test", updatedCachedUser.FullName);
            Assert.IsFalse(updatedCachedUser.Active);
            VerifyCacheHit(userCacheKey); // Cache should be rebuilt

            var users2 = _svc.GetAll();
            VerifyCacheHit("aio:users:all"); // Cache should be rebuilt
        }

        [TestMethod]
        public void TestUpdate_DatabaseFirst_ThenCacheInvalidation()
        {
            // Arrange
            const string id = "aio_db_update_test";
            var user = new Usr { Id = id, FullName = "Database Update Test", Active = true };
            _svc.Create(user);

            // Act - Update user
            var updatedUser = new Usr { Id = id, FullName = "Updated Database Test", Active = false };
            var msg = _svc.Update(updatedUser);

            // Assert
            Assert.IsNull(msg, "Update should succeed");

            // Verify database được update trước
            VerifyDatabaseHasUser(id);
            var dbUser = _dbService.GetById(id);
            Assert.IsNotNull(dbUser);
            Assert.AreEqual("Updated Database Test", dbUser.FullName);
            Assert.IsFalse(dbUser.Active);

            // Verify cache được invalidated sau
            VerifyCacheInvalidation(id);
        }

        [TestMethod]
        public void TestUpdate_CacheConsistency_AfterOperations()
        {
            // Arrange
            const string id = "aio_consistency_update_test";
            var user = new Usr { Id = id, FullName = "Consistency Update Test", Active = true };
            _svc.Create(user);

            // Cache user
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = $"aio:user:id:{id}";
            VerifyCacheHit(userCacheKey);

            // Act - Update user
            var updatedUser = new Usr { Id = id, FullName = "Updated Consistency Test", Active = false };
            var updateMsg = _svc.Update(updatedUser);
            Assert.IsNull(updateMsg, "Update should succeed");

            // Assert - Cache should be invalidated
            VerifyCacheMiss(userCacheKey);
            VerifyCacheMiss("aio:users:all");

            // Act - Get user again
            var freshUser = _svc.GetById(id);

            // Assert - Should get updated data
            Assert.IsNotNull(freshUser);
            Assert.AreEqual("Updated Consistency Test", freshUser.FullName);
            Assert.IsFalse(freshUser.Active);
            VerifyCacheHit(userCacheKey); // Should be cached again with new data
        }

        [TestMethod]
        public void TestUpdate_MultipleUpdates_CacheBehavior()
        {
            // Arrange
            const string id = "aio_multi_update_test";
            var user = new Usr { Id = id, FullName = "Multi Update Test", Active = true };
            _svc.Create(user);

            // Act - Multiple updates
            for (int i = 1; i <= 3; i++)
            {
                var updateUser = new Usr 
                { 
                    Id = id, 
                    FullName = $"Multi Update Test {i}", 
                    Active = i % 2 == 0 
                };
                
                var msg = _svc.Update(updateUser);
                Assert.IsNull(msg, $"Update {i} should succeed");

                // Verify data consistency
                var found = _dbService.GetById(id);
                Assert.IsNotNull(found);
                Assert.AreEqual($"Multi Update Test {i}", found.FullName);
                Assert.AreEqual(i % 2 == 0, found.Active);

                // Verify cache is invalidated after each update
                VerifyCacheInvalidation(id);
            }
        }

        [TestMethod]
        public void TestUpdate_Performance_WithCache()
        {
            // Arrange
            const string id = "aio_perf_update_test";
            var user = new Usr { Id = id, FullName = "Performance Update Test", Active = true };
            _svc.Create(user);

            // Measure update operation
            var updateTime = MeasureOperation(() => 
            {
                var updatedUser = new Usr { Id = id, FullName = "Updated Performance Test", Active = false };
                _svc.Update(updatedUser);
            });

            // Assert
            VerifyDatabaseHasUser(id);
            var dbUser = _dbService.GetById(id);
            Assert.AreEqual("Updated Performance Test", dbUser.FullName);

            Console.WriteLine($"Update operation took: {updateTime.TotalMilliseconds}ms");
        }
    }
}
