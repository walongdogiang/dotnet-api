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
    public sealed class MGetTests : BaseRedisTests<RdsUsrSvc>
    {
        [TestInitialize]
        public new void Setup()
        {
            base.Setup();
            ClearCache(); // Clear cache trước mỗi test
        }

        [DataTestMethod]
        [DataRow("rg01", "Redis Get User 1", true)]
        [DataRow("rg02", "Redis Get User 2", false)]
        [DataRow("rg03", "Redis Get User 3", true)]
        public void TestGetById_WithCache(string id, string fullName, bool active)
        {
            // Arrange - Tạo user trước
            var user = new Usr { Id = id, FullName = fullName, Active = active };
            var createMsg = _svc.Create(user);
            Assert.IsNull(createMsg, "User creation should succeed");

            var userCacheKey = string.Format("user:id:{0}", id);

            // Act - Lần đầu gọi (cache miss)
            var found1 = _svc.GetById(id);

            // Assert
            Assert.IsNotNull(found1, $"User with ID '{id}' should exist.");
            Assert.AreEqual(id, found1.Id, $"Id should match with '{id}'.");
            Assert.AreEqual(fullName, found1.FullName, $"Fullname should match with '{fullName}'.");
            Assert.AreEqual(active, found1.Active, $"Active status should match with '{active}'.");

            // Verify cache hit
            VerifyCacheHit(userCacheKey);

            // Act - Lần hai gọi (cache hit)
            var found2 = _svc.GetById(id);

            // Assert - Data should be same
            Assert.IsNotNull(found2);
            Assert.AreEqual(found1.Id, found2.Id);
            Assert.AreEqual(found1.FullName, found2.FullName);
            Assert.AreEqual(found1.Active, found2.Active);

            Console.WriteLine($"Test GetById with Cache: Id={found1.Id} | Fullname={found1.FullName} | active={found1.Active}");
        }

        [TestMethod]
        public void TestGetById_NotFound_ShouldReturnNull()
        {
            // Arrange
            const string nonExistentId = "rg_not_found";

            // Act
            var found = _svc.GetById(nonExistentId);

            // Assert
            Assert.IsNull(found, "Non-existent user should return null");

            // Verify no cache entry created for non-existent user
            var userCacheKey = string.Format("user:id:{0}", nonExistentId);
            VerifyCacheMiss(userCacheKey);
        }

        [TestMethod]
        public void TestGetById_InvalidId_ShouldReturnNull()
        {
            // Test với null ID
            var found1 = _svc.GetById(null);
            Assert.IsNull(found1, "Null ID should return null");

            // Test với empty ID
            var found2 = _svc.GetById("");
            Assert.IsNull(found2, "Empty ID should return null");

            // Test với whitespace ID
            var found3 = _svc.GetById("   ");
            Assert.IsNull(found3, "Whitespace ID should return null");
        }

        [TestMethod]
        public void TestGetAll_WithCache()
        {
            // Arrange - Tạo một số users
            var users = new[]
            {
                new Usr { Id = "rg_all_1", FullName = "Redis All User 1", Active = true },
                new Usr { Id = "rg_all_2", FullName = "Redis All User 2", Active = false },
                new Usr { Id = "rg_all_3", FullName = "Redis All User 3", Active = true }
            };

            foreach (var user in users)
            {
                var msg = _svc.Create(user);
                Assert.IsNull(msg, $"Creating user {user.Id} should succeed");
            }

            const string cacheKey = "users:all";

            // Act - Lần đầu gọi (cache miss)
            var allUsers1 = _svc.GetAll();

            // Assert
            Assert.IsNotNull(allUsers1, "GetAll should return non-null list");
            Assert.IsTrue(allUsers1.Count >= users.Length, "Should return at least the created users");

            // Verify cache hit
            VerifyCacheHit(cacheKey);

            // Act - Lần hai gọi (cache hit)
            var allUsers2 = _svc.GetAll();

            // Assert - Data should be same
            Assert.IsNotNull(allUsers2);
            Assert.AreEqual(allUsers1.Count, allUsers2.Count, "Cache hit should return same count");

            Console.WriteLine($"[COUNT] users = {allUsers1.Count}");
        }

        [TestMethod]
        public void TestGetByKwd_WithCache()
        {
            // Arrange - Tạo users với tên có keyword
            var users = new[]
            {
                new Usr { Id = "rg_kwd_1", FullName = "Nguyen Van Redis", Active = true },
                new Usr { Id = "rg_kwd_2", FullName = "Tran Thi Redis", Active = false },
                new Usr { Id = "rg_kwd_3", FullName = "Le Van Redis", Active = true },
                new Usr { Id = "rg_kwd_4", FullName = "Pham Van Test", Active = true } // Không có keyword "Redis"
            };

            foreach (var user in users)
            {
                var msg = _svc.Create(user);
                Assert.IsNull(msg, $"Creating user {user.Id} should succeed");
            }

            const string keyword = "Redis";
            var cacheKey = string.Format("users:keyword:{0}", keyword.ToLowerInvariant());

            // Act - Lần đầu gọi (cache miss)
            var foundUsers1 = _svc.GetByKwd(keyword);

            // Assert
            Assert.IsNotNull(foundUsers1, "GetByKwd should return non-null list");
            Assert.AreEqual(3, foundUsers1.Count, $"Should find 3 users with keyword '{keyword}'");

            // Verify cache hit
            VerifyCacheHit(cacheKey);

            // Act - Lần hai gọi (cache hit)
            var foundUsers2 = _svc.GetByKwd(keyword);

            // Assert - Data should be same
            Assert.IsNotNull(foundUsers2);
            Assert.AreEqual(foundUsers1.Count, foundUsers2.Count, "Cache hit should return same count");

            Console.WriteLine($"Test GetByKwd with Cache: keyword='{keyword}' | Found {foundUsers1.Count} users");
        }

        [TestMethod]
        public void TestGetByKwd_EmptyKeyword_ShouldReturnEmptyList()
        {
            // Test với null keyword
            var users1 = _svc.GetByKwd(null);
            Assert.IsNotNull(users1);
            Assert.AreEqual(0, users1.Count, "Null keyword should return empty list");

            // Test với empty keyword
            var users2 = _svc.GetByKwd("");
            Assert.IsNotNull(users2);
            Assert.AreEqual(0, users2.Count, "Empty keyword should return empty list");

            // Test với whitespace keyword
            var users3 = _svc.GetByKwd("   ");
            Assert.IsNotNull(users3);
            Assert.AreEqual(0, users3.Count, "Whitespace keyword should return empty list");
        }

        [TestMethod]
        public void TestGetByKwd_NotFound_ShouldReturnEmptyList()
        {
            // Arrange
            const string nonExistentKeyword = "NonExistentKeyword123";

            // Act
            var users = _svc.GetByKwd(nonExistentKeyword);

            // Assert
            Assert.IsNotNull(users);
            Assert.AreEqual(0, users.Count, "Non-existent keyword should return empty list");

            // Verify cache entry created for empty result
            var cacheKey = string.Format("users:keyword:{0}", nonExistentKeyword.ToLowerInvariant());
            VerifyCacheHit(cacheKey);
        }

        [TestMethod]
        public void TestCache_Expiration_Behavior()
        {
            // Arrange
            const string id = "cache_expiry_test";
            var user = new Usr { Id = id, FullName = "Cache Expiry Test", Active = true };
            _svc.Create(user);

            var userCacheKey = string.Format("user:id:{0}", id);

            // Act - Cache user
            var found1 = _svc.GetById(id);
            Assert.IsNotNull(found1);
            VerifyCacheHit(userCacheKey);

            // Simulate cache expiration by manually removing from cache
            _redisCache.Remove(userCacheKey);
            VerifyCacheMiss(userCacheKey);

            // Act - Get user again (should reload from fallback)
            var found2 = _svc.GetById(id);

            // Assert
            Assert.IsNotNull(found2);
            Assert.AreEqual(found1.Id, found2.Id);
            Assert.AreEqual(found1.FullName, found2.FullName);
            VerifyCacheHit(userCacheKey); // Should be cached again
        }

        [TestMethod]
        public void TestCache_Consistency_AfterOperations()
        {
            // Arrange
            const string id = "consistency_test";
            var user = new Usr { Id = id, FullName = "Consistency Test", Active = true };
            _svc.Create(user);

            // Cache user
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheHit(userCacheKey);

            // Act - Update user
            var updatedUser = new Usr { Id = id, FullName = "Updated Consistency Test", Active = false };
            var updateMsg = _svc.Update(updatedUser);
            Assert.IsNull(updateMsg, "Update should succeed");

            // Assert - Cache should be invalidated
            VerifyCacheMiss(userCacheKey);

            // Act - Get user again
            var freshUser = _svc.GetById(id);

            // Assert - Should get updated data
            Assert.IsNotNull(freshUser);
            Assert.AreEqual("Updated Consistency Test", freshUser.FullName);
            Assert.IsFalse(freshUser.Active);
            VerifyCacheHit(userCacheKey); // Should be cached again with new data
        }

        [TestMethod]
        public void TestCache_Performance_Comparison()
        {
            // Arrange - Tạo user
            const string id = "perf_test";
            var user = new Usr { Id = id, FullName = "Performance Test", Active = true };
            _svc.Create(user);

            // Measure first call (cache miss)
            var start1 = DateTime.UtcNow;
            var found1 = _svc.GetById(id);
            var time1 = DateTime.UtcNow - start1;

            // Measure second call (cache hit)
            var start2 = DateTime.UtcNow;
            var found2 = _svc.GetById(id);
            var time2 = DateTime.UtcNow - start2;

            // Assert
            Assert.IsNotNull(found1);
            Assert.IsNotNull(found2);
            Assert.AreEqual(found1.Id, found2.Id);

            // Cache hit should be faster (though this might not always be true in test environment)
            Console.WriteLine($"First call (cache miss): {time1.TotalMilliseconds}ms");
            Console.WriteLine($"Second call (cache hit): {time2.TotalMilliseconds}ms");
        }
    }
}
