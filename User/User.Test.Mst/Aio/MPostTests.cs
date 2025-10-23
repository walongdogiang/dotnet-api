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
    public sealed class MPostTests : BaseAioTests<AioUsrSvc>
    {
        [TestInitialize]
        public new void Setup()
        {
            base.Setup();
            ClearCache(); // Clear cache trước mỗi test
        }

        [DataTestMethod]
        [DataRow("aio01", "Aio User A", "123 Aio Street", "1990-01-01", "Description for Aio User A", true)]
        [DataRow("aio02", "Aio User B", "456 Aio Avenue", "1992-02-02", "Description for Aio User B", false)]
        public void TestCreate_WithCacheAndDatabase(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            // Arrange
            var countBefore = _dbService.GetAll().Count;
            var allUsersCacheKey = "aio:users:all";

            // Act - Tạo user
            var msg = _svc.Create(new Usr 
            { 
                Id = id, 
                FullName = fullName, 
                Address = address, 
                BirthDay = DateTime.Parse(birthDay), 
                Description = description, 
                Active = active 
            });

            // Assert - Kiểm tra tạo thành công
            Assert.IsNull(msg, "Create should return null on success.");
            
            var countAfter = _dbService.GetAll().Count;
            Assert.AreEqual(countBefore + 1, countAfter, "Database count should increase by 1");

            // Verify user được tạo trong database
            VerifyDatabaseHasUser(id);
            var found = _dbService.GetById(id);
            Assert.IsNotNull(found, "User should exist in database");
            Assert.AreEqual(id, found.Id);
            Assert.AreEqual(fullName, found.FullName);
            Assert.AreEqual(active, found.Active);

            // Verify cache invalidation (cache should be cleared after create)
            VerifyCacheMiss(allUsersCacheKey);

            // Test cache behavior - lần đầu gọi GetAll sẽ cache lại
            var users = _svc.GetAll();
            Assert.IsNotNull(users);
            Assert.IsTrue(users.Count >= countAfter);
            VerifyCacheHit(allUsersCacheKey);

            Console.WriteLine($"Test Create with Cache & DB: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
        }

        [TestMethod]
        public void TestCreate_Duplicate_ShouldReturnError()
        {
            // Arrange
            var id = "aio_dup_01";
            var newUsr = new Usr { Id = id, FullName = "Aio Duplicate User 1", Active = true };
            
            // Act - Tạo lần đầu
            var msg1 = _svc.Create(newUsr);
            Assert.IsNull(msg1, "First create should succeed.");

            var count = _dbService.GetAll().Count;

            // Act - Tạo lần hai với cùng ID
            var msg2 = _svc.Create(new Usr { Id = id, FullName = "Aio Duplicate User 2", Active = false });

            // Assert
            Assert.IsNotNull(msg2, "Second create with duplicate ID should return error message.");
            Assert.AreEqual(count, _dbService.GetAll().Count, "Database count should not change after duplicate create");
            
            // Verify user vẫn giữ giá trị ban đầu trong database
            var found = _dbService.GetById(id);
            Assert.IsNotNull(found);
            Assert.AreEqual("Aio Duplicate User 1", found.FullName);
            Assert.IsTrue(found.Active);
        }

        [TestMethod]
        public void TestCreate_InvalidUser_ShouldReturnError()
        {
            // Test với user null
            var msg1 = _svc.Create(null);
            Assert.IsNotNull(msg1, "Creating null user should return error");

            // Test với ID null
            var msg2 = _svc.Create(new Usr { Id = null, FullName = "Test", Active = true });
            Assert.IsNotNull(msg2, "Creating user with null ID should return error");

            // Test với FullName null
            var msg3 = _svc.Create(new Usr { Id = "test", FullName = null, Active = true });
            Assert.IsNotNull(msg3, "Creating user with null FullName should return error");

            // Test với ID empty
            var msg4 = _svc.Create(new Usr { Id = "", FullName = "Test", Active = true });
            Assert.IsNotNull(msg4, "Creating user with empty ID should return error");

            // Test với FullName empty
            var msg5 = _svc.Create(new Usr { Id = "test", FullName = "", Active = true });
            Assert.IsNotNull(msg5, "Creating user with empty FullName should return error");
        }

        [TestMethod]
        public void TestCreate_CacheInvalidation_Behavior()
        {
            // Arrange - Tạo một user trước
            var user1 = new Usr { Id = "aio_cache_test_1", FullName = "Cache Test User 1", Active = true };
            _svc.Create(user1);

            // Act - Gọi GetAll để cache data
            var users1 = _svc.GetAll();
            VerifyCacheHit("aio:users:all");

            // Act - Tạo user mới
            var user2 = new Usr { Id = "aio_cache_test_2", FullName = "Cache Test User 2", Active = true };
            var msg = _svc.Create(user2);

            // Assert
            Assert.IsNull(msg, "Create should succeed");
            VerifyCacheMiss("aio:users:all"); // Cache should be invalidated

            // Act - Gọi GetAll lại để verify cache được rebuild
            var users2 = _svc.GetAll();
            Assert.IsTrue(users2.Count > users1.Count, "User count should increase");
            VerifyCacheHit("aio:users:all"); // Cache should be rebuilt
        }

        [TestMethod]
        public void TestCreate_DatabaseFirst_ThenCacheInvalidation()
        {
            // Arrange
            const string id = "aio_db_first_test";
            var user = new Usr { Id = id, FullName = "Database First Test", Active = true };

            // Act - Tạo user
            var msg = _svc.Create(user);

            // Assert
            Assert.IsNull(msg, "Create should succeed");

            // Verify database được update trước
            VerifyDatabaseHasUser(id);
            var dbUser = _dbService.GetById(id);
            Assert.IsNotNull(dbUser);
            Assert.AreEqual("Database First Test", dbUser.FullName);

            // Verify cache được invalidated sau
            VerifyCacheInvalidation(id);
        }

        [TestMethod]
        public void TestCreate_MultipleUsers_CacheBehavior()
        {
            // Arrange
            var userIds = new[] { "aio_multi_1", "aio_multi_2", "aio_multi_3" };
            var initialCount = _dbService.GetAll().Count;

            // Act - Tạo nhiều users
            foreach (var id in userIds)
            {
                var user = new Usr { Id = id, FullName = $"Multi User {id}", Active = true };
                var msg = _svc.Create(user);
                Assert.IsNull(msg, $"Creating user {id} should succeed");

                // Verify cache bị invalidated sau mỗi create
                VerifyCacheMiss("aio:users:all");
            }

            // Assert - Tất cả users được tạo trong database
            var finalCount = _dbService.GetAll().Count;
            Assert.AreEqual(initialCount + userIds.Length, finalCount, "All users should be created in database");

            // Verify cache được rebuild
            var users = _svc.GetAll();
            VerifyCacheHit("aio:users:all");
        }

        [TestMethod]
        public void TestCreate_Performance_WithCache()
        {
            // Arrange
            const string id = "aio_perf_test";
            var user = new Usr { Id = id, FullName = "Performance Test User", Active = true };

            // Measure create operation
            var createTime = MeasureOperation(() => _svc.Create(user));

            // Assert
            Assert.IsNull(_svc.Create(user), "Create should succeed");
            VerifyDatabaseHasUser(id);

            Console.WriteLine($"Create operation took: {createTime.TotalMilliseconds}ms");
        }

        [TestMethod]
        public void TestCreate_CacheConsistency_AfterOperations()
        {
            // Arrange
            const string id = "aio_consistency_test";
            var user = new Usr { Id = id, FullName = "Consistency Test User", Active = true };

            // Act - Tạo user
            var createMsg = _svc.Create(user);
            Assert.IsNull(createMsg, "Create should succeed");

            // Verify database consistency
            VerifyDatabaseHasUser(id);
            var dbUser = _dbService.GetById(id);
            Assert.AreEqual("Consistency Test User", dbUser.FullName);

            // Verify cache invalidation
            VerifyCacheInvalidation(id);

            // Act - Gọi GetAll để rebuild cache
            var users = _svc.GetAll();
            Assert.IsNotNull(users);
            VerifyCacheHit("aio:users:all");

            // Verify cache contains the new user
            var cachedUser = users.FirstOrDefault(u => u.Id == id);
            Assert.IsNotNull(cachedUser, "New user should be in cached data");
            Assert.AreEqual("Consistency Test User", cachedUser.FullName);
        }
    }
}
