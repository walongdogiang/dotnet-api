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
    public sealed class MDelTests : BaseAioTests<AioUsrSvc>
    {
        [TestInitialize]
        public new void Setup()
        {
            base.Setup();
            ClearCache(); // Clear cache trước mỗi test
        }

        [DataTestMethod]
        [DataRow("ad01")]
        [DataRow("ad02")]
        public void TestDelete_WithCacheAndDatabase(string id)
        {
            // Arrange - Tạo user trước
            var newUser = new Usr { Id = id, FullName = "Aio Delete Test User", Active = true };
            var createMsg = _svc.Create(newUser);
            Assert.IsNull(createMsg, "New user should be created without error.");

            var countBefore = _dbService.GetAll().Count;
            var userCacheKey = $"aio:user:id:{id}";

            // Cache user trước khi xóa
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            VerifyCacheHit(userCacheKey);

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);

            // Assert
            Assert.IsNull(deleteMsg, "Delete existing should return null.");
            Assert.AreEqual(countBefore - 1, _dbService.GetAll().Count, "Database count should decrease by 1.");
            Assert.IsNull(_dbService.GetById(id), "Deleted user must not be found in database.");

            // Verify cache invalidation
            VerifyCacheInvalidation(id);

            Console.WriteLine($"Test Delete with Cache & DB: Deleted user {id}");
        }

        [TestMethod]
        public void TestDelete_NotFound_ShouldReturnError()
        {
            // Arrange
            const string nonExistentId = "ad_not_found";
            var before = _dbService.GetAll().Count;

            // Act
            var msg = _svc.DelById(nonExistentId);

            // Assert
            Assert.IsNotNull(msg, "Deleting non-existing should return error message.");
            Assert.AreEqual(before, _dbService.GetAll().Count, "Database count must not change when deleting non-existing user.");

            // Verify no cache operations occurred
            var userCacheKey = $"aio:user:id:{nonExistentId}";
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
            const string id = "aio_cache_del_test";
            var user = new Usr { Id = id, FullName = "Cache Delete Test", Active = true };
            _svc.Create(user);

            // Cache user
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = $"aio:user:id:{id}";
            VerifyCacheHit(userCacheKey);

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("aio:users:all");

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);

            // Assert
            Assert.IsNull(deleteMsg, "Delete should succeed");
            VerifyCacheInvalidation(id);

            // Verify user is deleted from database
            VerifyDatabaseNoUser(id);

            // Verify cache gets rebuilt for GetAll
            var users2 = _svc.GetAll();
            Assert.IsTrue(users2.Count < users1.Count, "User count should decrease");
            VerifyCacheHit("aio:users:all"); // Cache should be rebuilt
        }

        [TestMethod]
        public void TestDelete_DatabaseFirst_ThenCacheInvalidation()
        {
            // Arrange
            const string id = "aio_db_del_test";
            var user = new Usr { Id = id, FullName = "Database Delete Test", Active = true };
            _svc.Create(user);

            // Verify user exists in database
            VerifyDatabaseHasUser(id);

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);

            // Assert
            Assert.IsNull(deleteMsg, "Delete should succeed");

            // Verify database được update trước
            VerifyDatabaseNoUser(id);

            // Verify cache được invalidated sau
            VerifyCacheInvalidation(id);
        }

        [TestMethod]
        public void TestDelete_MultipleUsers_WithCache()
        {
            // Arrange - Tạo nhiều users
            var userIds = new[] { "ad_multi_1", "ad_multi_2", "ad_multi_3" };
            var initialCount = _dbService.GetAll().Count;

            foreach (var id in userIds)
            {
                var user = new Usr { Id = id, FullName = $"Multi Delete User {id}", Active = true };
                var createMsg = _svc.Create(user);
                Assert.IsNull(createMsg, $"Creating user {id} should succeed");
            }

            var countAfterCreate = _dbService.GetAll().Count;
            Assert.AreEqual(initialCount + userIds.Length, countAfterCreate, "All users should be created in database");

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("aio:users:all");

            // Act - Xóa từng user
            foreach (var id in userIds)
            {
                var userCacheKey = $"aio:user:id:{id}";
                
                // Cache user trước khi xóa
                var cachedUser = _svc.GetById(id);
                Assert.IsNotNull(cachedUser);
                VerifyCacheHit(userCacheKey);

                // Xóa user
                var deleteMsg = _svc.DelById(id);
                Assert.IsNull(deleteMsg, $"Deleting user {id} should succeed");

                // Verify cache invalidation
                VerifyCacheInvalidation(id);
            }

            // Assert - Tất cả users đã bị xóa
            var finalCount = _dbService.GetAll().Count;
            Assert.AreEqual(initialCount, finalCount, "All test users should be deleted from database");

            // Verify cache gets rebuilt
            var users2 = _svc.GetAll();
            VerifyCacheHit("aio:users:all");
        }

        [TestMethod]
        public void TestDelete_AndRecreate_WithCache()
        {
            // Arrange
            const string id = "ad_recreate_test";
            var user1 = new Usr { Id = id, FullName = "Original User", Active = true };
            _svc.Create(user1);

            // Cache user
            var cachedUser1 = _svc.GetById(id);
            Assert.IsNotNull(cachedUser1);
            var userCacheKey = $"aio:user:id:{id}";
            VerifyCacheHit(userCacheKey);

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);
            Assert.IsNull(deleteMsg, "Delete should succeed");
            VerifyCacheInvalidation(id);

            // Act - Tạo lại user với cùng ID
            var user2 = new Usr { Id = id, FullName = "Recreated User", Active = false };
            var createMsg = _svc.Create(user2);
            Assert.IsNull(createMsg, "Recreate should succeed");

            // Assert - User mới được tạo trong database
            VerifyDatabaseHasUser(id);
            var recreatedUser = _dbService.GetById(id);
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
            const string id = "ad_consistency_test";
            var user = new Usr { Id = id, FullName = "Consistency Test", Active = true };
            _svc.Create(user);

            // Cache user
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = $"aio:user:id:{id}";
            VerifyCacheHit(userCacheKey);

            // Act - Update user trước khi xóa
            var updatedUser = new Usr { Id = id, FullName = "Updated Before Delete", Active = false };
            var updateMsg = _svc.Update(updatedUser);
            Assert.IsNull(updateMsg, "Update should succeed");
            VerifyCacheInvalidation(id); // Cache should be invalidated after update

            // Cache lại sau update
            var updatedCachedUser = _svc.GetById(id);
            Assert.IsNotNull(updatedCachedUser);
            Assert.AreEqual("Updated Before Delete", updatedCachedUser.FullName);
            VerifyCacheHit(userCacheKey);

            // Act - Xóa user
            var deleteMsg = _svc.DelById(id);
            Assert.IsNull(deleteMsg, "Delete should succeed");

            // Assert - Cache should be invalidated
            VerifyCacheInvalidation(id);

            // Verify user is deleted from database
            VerifyDatabaseNoUser(id);
        }

        [TestMethod]
        public void TestDelete_AllUsers_WithCache()
        {
            // Arrange - Tạo một số users
            var userIds = new[] { "ad_all_1", "ad_all_2", "ad_all_3" };
            foreach (var id in userIds)
            {
                var user = new Usr { Id = id, FullName = $"All Delete User {id}", Active = true };
                _svc.Create(user);
            }

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("aio:users:all");

            // Act - Xóa tất cả users
            foreach (var id in userIds)
            {
                var deleteMsg = _svc.DelById(id);
                Assert.IsNull(deleteMsg, $"Deleting user {id} should succeed");
            }

            // Assert - Cache should be invalidated
            VerifyCacheMiss("aio:users:all");

            // Verify all users are deleted from database
            foreach (var id in userIds)
            {
                VerifyDatabaseNoUser(id);
            }

            // Verify cache gets rebuilt
            var users2 = _svc.GetAll();
            VerifyCacheHit("aio:users:all");
        }

        [TestMethod]
        public void TestDelete_Performance_WithCache()
        {
            // Arrange - Tạo user
            const string id = "ad_perf_test";
            var user = new Usr { Id = id, FullName = "Performance Delete Test", Active = true };
            _svc.Create(user);

            // Cache user
            var cachedUser = _svc.GetById(id);
            Assert.IsNotNull(cachedUser);
            var userCacheKey = $"aio:user:id:{id}";
            VerifyCacheHit(userCacheKey);

            // Measure delete operation
            var deleteTime = MeasureOperation(() => _svc.DelById(id));

            // Assert
            Assert.IsNull(_svc.DelById(id), "Delete should succeed");
            VerifyDatabaseNoUser(id);
            VerifyCacheInvalidation(id);

            Console.WriteLine($"Delete operation took: {deleteTime.TotalMilliseconds}ms");
        }

        [TestMethod]
        public void TestDelete_CacheRebuild_AfterInvalidation()
        {
            // Arrange - Tạo users
            var users = new[]
            {
                new Usr { Id = "ad_rebuild_1", FullName = "Delete Rebuild User 1", Active = true },
                new Usr { Id = "ad_rebuild_2", FullName = "Delete Rebuild User 2", Active = false }
            };

            foreach (var user in users)
            {
                _svc.Create(user);
            }

            // Cache all users
            var users1 = _svc.GetAll();
            VerifyCacheHit("aio:users:all");

            // Act - Xóa một user (should invalidate cache)
            _svc.DelById("ad_rebuild_1");

            // Assert - Cache should be invalidated
            VerifyCacheMiss("aio:users:all");

            // Act - Get all users again (should rebuild cache)
            var users2 = _svc.GetAll();

            // Assert - Cache should be rebuilt with remaining data
            Assert.IsNotNull(users2);
            Assert.IsTrue(users2.Count < users1.Count, "Should have fewer users after delete");
            VerifyCacheHit("aio:users:all");
        }
    }
}
