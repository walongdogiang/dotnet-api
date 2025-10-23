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
    public sealed class MDelTests : BaseRedisTests<RdsUsrSvc>
    {
        [TestInitialize]
        public new void Setup()
        {
            base.Setup();
            ClearCache(); // Clear cache trước mỗi test
        }

        [DataTestMethod]
        [DataRow("rd01")]
        [DataRow("rd02")]
        public void TestDelete_WithCache(string id)
        {
            // Arrange - Tạo user trước
            var newUser = new Usr { Id = id, FullName = "Redis Delete Test User", Active = true };
            var createMsg = _svc.Create(newUser);
            Assert.IsNull(createMsg, "New user should be created without error.");

            var countBefore = _fallbackService.GetAll().Count;
            var userCacheKey = string.Format("user:id:{0}", id);

            // Cache user trước khi xóa
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            VerifyCacheHit(userCacheKey);

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);

            // Assert
            Assert.IsNull(deleteMsg, "Delete existing should return null.");
            Assert.AreEqual(countBefore - 1, _fallbackService.GetAll().Count, "User count should decrease by 1.");
            Assert.IsNull(_fallbackService.GetById(id), "Deleted user must not be found in fallback service.");

            // Verify cache invalidation
            VerifyCacheMiss(userCacheKey); // User cache should be invalidated
            VerifyCacheMiss("users:all"); // All users cache should be invalidated

            Console.WriteLine($"Test Delete with Cache: Deleted user {id}");
        }

        [TestMethod]
        public void TestDelete_NotFound_ShouldReturnError()
        {
            // Arrange
            const string nonExistentId = "rd_not_found";
            var before = _fallbackService.GetAll().Count;

            // Act
            var msg = _svc.DelById(nonExistentId);

            // Assert
            Assert.IsNotNull(msg, "Deleting non-existing should return error message.");
            Assert.AreEqual(before, _fallbackService.GetAll().Count, "Count must not change when deleting non-existing user.");

            // Verify no cache operations occurred
            var userCacheKey = string.Format("user:id:{0}", nonExistentId);
            VerifyCacheMiss(userCacheKey);
        }

        [TestMethod]
        public void TestDelete_InvalidId_ShouldReturnError()
        {
            // Test với null ID
            var msg1 = _svc.DelById(null);
            Assert.IsNotNull(msg1, "Deleting with null ID should return error");

            // Test với empty ID
            var msg2 = _svc.DelById("");
            Assert.IsNotNull(msg2, "Deleting with empty ID should return error");

            // Test với whitespace ID
            var msg3 = _svc.DelById("   ");
            Assert.IsNotNull(msg3, "Deleting with whitespace ID should return error");
        }

        [TestMethod]
        public void TestDelete_CacheInvalidation_Behavior()
        {
            // Arrange - Tạo user
            const string id = "cache_del_test";
            var user = new Usr { Id = id, FullName = "Cache Delete Test", Active = true };
            _svc.Create(user);

            // Cache user
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheHit(userCacheKey);

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("users:all");

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);

            // Assert
            Assert.IsNull(deleteMsg, "Delete should succeed");
            VerifyCacheMiss(userCacheKey); // User cache should be invalidated
            VerifyCacheMiss("users:all"); // All users cache should be invalidated

            // Verify user is deleted
            var deletedUser = _fallbackService.GetById(id);
            Assert.IsNull(deletedUser, "User should be deleted from fallback service");

            // Verify cache gets rebuilt for GetAll
            var users2 = _svc.GetAll();
            Assert.IsTrue(users2.Count < users1.Count, "User count should decrease");
            VerifyCacheHit("users:all"); // Cache should be rebuilt
        }

        [TestMethod]
        public void TestDelete_MultipleUsers_WithCache()
        {
            // Arrange - Tạo nhiều users
            var userIds = new[] { "rd_multi_1", "rd_multi_2", "rd_multi_3" };
            var initialCount = _fallbackService.GetAll().Count;

            foreach (var id in userIds)
            {
                var user = new Usr { Id = id, FullName = $"Multi Delete User {id}", Active = true };
                var createMsg = _svc.Create(user);
                Assert.IsNull(createMsg, $"Creating user {id} should succeed");
            }

            var countAfterCreate = _fallbackService.GetAll().Count;
            Assert.AreEqual(initialCount + userIds.Length, countAfterCreate, "All users should be created");

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("users:all");

            // Act - Xóa từng user
            foreach (var id in userIds)
            {
                var userCacheKey = string.Format("user:id:{0}", id);
                
                // Cache user trước khi xóa
                var cachedUser = _svc.GetById(id);
                Assert.IsNotNull(cachedUser);
                VerifyCacheHit(userCacheKey);

                // Xóa user
                var deleteMsg = _svc.DelById(id);
                Assert.IsNull(deleteMsg, $"Deleting user {id} should succeed");

                // Verify cache invalidation
                VerifyCacheMiss(userCacheKey);
                VerifyCacheMiss("users:all");
            }

            // Assert - Tất cả users đã bị xóa
            var finalCount = _fallbackService.GetAll().Count;
            Assert.AreEqual(initialCount, finalCount, "All test users should be deleted");

            // Verify cache gets rebuilt
            var users2 = _svc.GetAll();
            VerifyCacheHit("users:all");
        }

        [TestMethod]
        public void TestDelete_AndRecreate_WithCache()
        {
            // Arrange
            const string id = "rd_recreate_test";
            var user1 = new Usr { Id = id, FullName = "Original User", Active = true };
            _svc.Create(user1);

            // Cache user
            var cachedUser1 = _svc.GetById(id);
            Assert.IsNotNull(cachedUser1);
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheHit(userCacheKey);

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);
            Assert.IsNull(deleteMsg, "Delete should succeed");
            VerifyCacheMiss(userCacheKey);

            // Act - Tạo lại user với cùng ID
            var user2 = new Usr { Id = id, FullName = "Recreated User", Active = false };
            var createMsg = _svc.Create(user2);
            Assert.IsNull(createMsg, "Recreate should succeed");

            // Assert - User mới được tạo
            var recreatedUser = _fallbackService.GetById(id);
            Assert.IsNotNull(recreatedUser);
            Assert.AreEqual("Recreated User", recreatedUser.FullName);
            Assert.IsFalse(recreatedUser.Active);

            // Verify cache behavior
            var cachedUser2 = _svc.GetById(id);
            Assert.IsNotNull(cachedUser2);
            Assert.AreEqual("Recreated User", cachedUser2.FullName);
            VerifyCacheHit(userCacheKey); // Should be cached again
        }

        [TestMethod]
        public void TestDelete_CacheConsistency_AfterOperations()
        {
            // Arrange
            const string id = "rd_consistency_test";
            var user = new Usr { Id = id, FullName = "Consistency Test", Active = true };
            _svc.Create(user);

            // Cache user
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheHit(userCacheKey);

            // Act - Update user trước khi xóa
            var updatedUser = new Usr { Id = id, FullName = "Updated Before Delete", Active = false };
            var updateMsg = _svc.Update(updatedUser);
            Assert.IsNull(updateMsg, "Update should succeed");
            VerifyCacheMiss(userCacheKey); // Cache should be invalidated after update

            // Cache lại sau update
            var updatedCachedUser = _svc.GetById(id);
            Assert.IsNotNull(updatedCachedUser);
            Assert.AreEqual("Updated Before Delete", updatedCachedUser.FullName);
            VerifyCacheHit(userCacheKey);

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);
            Assert.IsNull(deleteMsg, "Delete should succeed");

            // Assert - Cache should be invalidated
            VerifyCacheMiss(userCacheKey);
            VerifyCacheMiss("users:all");

            // Verify user is deleted
            var deletedUser = _fallbackService.GetById(id);
            Assert.IsNull(deletedUser, "User should be deleted");
        }

        [TestMethod]
        public void TestDelete_AllUsers_WithCache()
        {
            // Arrange - Tạo một số users
            var userIds = new[] { "rd_all_1", "rd_all_2", "rd_all_3" };
            foreach (var id in userIds)
            {
                var user = new Usr { Id = id, FullName = $"All Delete User {id}", Active = true };
                _svc.Create(user);
            }

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("users:all");

            // Act - Xóa tất cả users
            foreach (var id in userIds)
            {
                var deleteMsg = _svc.DelById(id);
                Assert.IsNull(deleteMsg, $"Deleting user {id} should succeed");
            }

            // Assert - Cache should be invalidated
            VerifyCacheMiss("users:all");

            // Verify all users are deleted
            foreach (var id in userIds)
            {
                var user = _fallbackService.GetById(id);
                Assert.IsNull(user, $"User {id} should be deleted");
            }

            // Verify cache gets rebuilt
            var users2 = _svc.GetAll();
            VerifyCacheHit("users:all");
        }

        [TestMethod]
        public void TestDelete_Performance_WithCache()
        {
            // Arrange - Tạo user
            const string id = "rd_perf_test";
            var user = new Usr { Id = id, FullName = "Performance Delete Test", Active = true };
            _svc.Create(user);

            // Cache user
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = string.Format("user:id:{0}", id);
            VerifyCacheHit(userCacheKey);

            // Measure delete operation
            var start = DateTime.UtcNow;
            var deleteMsg = _svc.DelById(id);
            var deleteTime = DateTime.UtcNow - start;

            // Assert
            Assert.IsNull(deleteMsg, "Delete should succeed");
            VerifyCacheMiss(userCacheKey);

            Console.WriteLine($"Delete operation took: {deleteTime.TotalMilliseconds}ms");
        }
    }
}
