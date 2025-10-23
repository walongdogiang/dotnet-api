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
    public sealed class MPostTests : BaseRedisTests<RdsUsrSvc>
    {
        [TestInitialize]
        public new void Setup()
        {
            base.Setup();
            ClearCache(); // Clear cache trước mỗi test
        }

        [DataTestMethod]
        [DataRow("r01", "Redis User A", "123 Redis Street", "1990-01-01", "Description for Redis User A", true)]
        [DataRow("r02", "Redis User B", "456 Redis Avenue", "1992-02-02", "Description for Redis User B", false)]
        public void TestCreate_WithCache(string id, string fullName, string address, string birthDay, string description, bool active)
        {
            // Arrange
            var countBefore = _fallbackService.GetAll().Count;
            var cacheKey = "users:all";

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
            
            var countAfter = _fallbackService.GetAll().Count;
            Assert.AreEqual(countBefore + 1, countAfter, "Fallback service count should increase by 1");

            // Verify user được tạo trong fallback service
            var found = _fallbackService.GetById(id);
            Assert.IsNotNull(found, "User should exist in fallback service");
            Assert.AreEqual(id, found.Id);
            Assert.AreEqual(fullName, found.FullName);
            Assert.AreEqual(active, found.Active);

            // Verify cache được invalidated (cache all users bị xóa)
            VerifyCacheMiss(cacheKey);

            // Test cache behavior - lần đầu gọi GetAll sẽ cache lại
            var users = _svc.GetAll();
            Assert.IsNotNull(users);
            Assert.IsTrue(users.Count >= countAfter);
            VerifyCacheHit(cacheKey);

            Console.WriteLine($"Test Create with Cache: id={found.Id} | Fullname={found.FullName} | Address={found.Address} | Birthday={found.BirthDay} | Description={found.Description} | Active={found.Active}");
        }

        [TestMethod]
        public void TestCreate_Duplicate_ShouldReturnError()
        {
            // Arrange
            var id = "dup_redis_01";
            var newUsr = new Usr { Id = id, FullName = "Redis Duplicate User 1", Active = true };
            
            // Act - Tạo lần đầu
            var msg1 = _svc.Create(newUsr);
            Assert.IsNull(msg1, "First create should succeed.");

            var count = _fallbackService.GetAll().Count;

            // Act - Tạo lần hai với cùng ID
            var msg2 = _svc.Create(new Usr { Id = id, FullName = "Redis Duplicate User 2", Active = false });

            // Assert
            Assert.IsNotNull(msg2, "Second create with duplicate ID should return error message.");
            Assert.AreEqual(count, _fallbackService.GetAll().Count, "Count should not change after duplicate create");
            
            // Verify user vẫn giữ giá trị ban đầu
            var found = _fallbackService.GetById(id);
            Assert.IsNotNull(found);
            Assert.AreEqual("Redis Duplicate User 1", found.FullName);
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
            var user1 = new Usr { Id = "cache_test_1", FullName = "Cache Test User 1", Active = true };
            _svc.Create(user1);

            // Act - Gọi GetAll để cache data
            var users1 = _svc.GetAll();
            VerifyCacheHit("users:all");

            // Act - Tạo user mới
            var user2 = new Usr { Id = "cache_test_2", FullName = "Cache Test User 2", Active = true };
            var msg = _svc.Create(user2);

            // Assert
            Assert.IsNull(msg, "Create should succeed");
            VerifyCacheMiss("users:all"); // Cache should be invalidated

            // Act - Gọi GetAll lại để verify cache được rebuild
            var users2 = _svc.GetAll();
            Assert.IsTrue(users2.Count > users1.Count, "User count should increase");
            VerifyCacheHit("users:all"); // Cache should be rebuilt
        }
    }
}
