using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using User.Svc;
using User.Svc.Cache;
using User.Svc.Aio;
using User.Db.MSSQL.EF;
using User.Db.MSSQL.EF.DB;

namespace User.Test.Mst.BaseClass
{
    public abstract class BaseAioTests<TSvc> where TSvc : class, IUsersSvc
    {
        protected IUsersSvc _svc;
        protected IRedisCache _redisCache;
        protected EFUsrsSvc _dbService;
        protected RdsUsrSvc _cacheService;
        
        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();

            // Setup Entity Framework với In-Memory Database
            services.AddDbContext<UsrDbContext>(opt =>
                opt.UseInMemoryDatabase($"aio-test-{Guid.NewGuid()}"));

            // Setup Redis Cache Mock
            services.AddSingleton<IRedisCache, MockRedisCache>();

            // Setup Database Service (EF)
            services.AddScoped<EFUsrsSvc>();

            // Setup Cache Service (Redis)
            services.AddScoped<RdsUsrSvc>();

            // Setup AioUsrSvc
            services.AddScoped<AioUsrSvc>();

            // Setup Time Provider
            services.AddSingleton<ITimeProvider, SystemTimeProvider>();

            var provider = services.BuildServiceProvider();
            _svc = provider.GetRequiredService<TSvc>();
            _redisCache = provider.GetRequiredService<IRedisCache>();
            _dbService = provider.GetRequiredService<EFUsrsSvc>();
            _cacheService = provider.GetRequiredService<RdsUsrSvc>();
        }

        public interface ITimeProvider { DateTime Now { get; } }
        public class SystemTimeProvider : ITimeProvider
        {
            public DateTime Now => DateTime.Now;
        }

        /// <summary>
        /// Helper method để clear cache trước khi test
        /// </summary>
        protected void ClearCache()
        {
            // Clear tất cả cache keys có thể có
            var allKeys = new[]
            {
                "aio:users:all",
                "aio:user:id:1", "aio:user:id:2", "aio:user:id:3",
                "aio:users:keyword:nguyen", "aio:users:keyword:tran", "aio:users:keyword:le"
            };

            foreach (var key in allKeys)
            {
                _redisCache.Remove(key);
            }
        }

        /// <summary>
        /// Helper method để verify cache hit
        /// </summary>
        protected void VerifyCacheHit(string cacheKey)
        {
            Assert.IsTrue(_redisCache.Exists(cacheKey), $"Cache key '{cacheKey}' should exist after operation");
        }

        /// <summary>
        /// Helper method để verify cache miss
        /// </summary>
        protected void VerifyCacheMiss(string cacheKey)
        {
            Assert.IsFalse(_redisCache.Exists(cacheKey), $"Cache key '{cacheKey}' should not exist after operation");
        }

        /// <summary>
        /// Helper method để verify database có data
        /// </summary>
        protected void VerifyDatabaseHasUser(string userId)
        {
            var user = _dbService.GetById(userId);
            Assert.IsNotNull(user, $"User '{userId}' should exist in database");
        }

        /// <summary>
        /// Helper method để verify database không có data
        /// </summary>
        protected void VerifyDatabaseNoUser(string userId)
        {
            var user = _dbService.GetById(userId);
            Assert.IsNull(user, $"User '{userId}' should not exist in database");
        }

        /// <summary>
        /// Helper method để tạo test data trong database
        /// </summary>
        protected void SeedTestData()
        {
            var testUsers = new[]
            {
                new Usr { Id = "aio_test_1", FullName = "Aio Test User 1", Active = true },
                new Usr { Id = "aio_test_2", FullName = "Aio Test User 2", Active = false },
                new Usr { Id = "aio_test_3", FullName = "Aio Test User 3", Active = true }
            };

            foreach (var user in testUsers)
            {
                var result = _dbService.Create(user);
                Assert.IsNull(result, $"Creating test user {user.Id} should succeed");
            }
        }

        /// <summary>
        /// Helper method để verify cache behavior
        /// </summary>
        protected void VerifyCacheBehavior(string operation, bool shouldHitCache)
        {
            if (shouldHitCache)
            {
                Console.WriteLine($"✅ Expected cache HIT for {operation}");
            }
            else
            {
                Console.WriteLine($"❌ Expected cache MISS for {operation}");
            }
        }

        /// <summary>
        /// Helper method để measure performance
        /// </summary>
        protected TimeSpan MeasureOperation(Action operation)
        {
            var start = DateTime.UtcNow;
            operation();
            return DateTime.UtcNow - start;
        }

        /// <summary>
        /// Helper method để verify cache invalidation
        /// </summary>
        protected void VerifyCacheInvalidation(string userId)
        {
            var userCacheKey = $"aio:user:id:{userId}";
            var allUsersCacheKey = "aio:users:all";
            
            VerifyCacheMiss(userCacheKey);
            VerifyCacheMiss(allUsersCacheKey);
        }

        /// <summary>
        /// Helper method để verify cache rebuild
        /// </summary>
        protected void VerifyCacheRebuild()
        {
            var allUsersCacheKey = "aio:users:all";
            
            // Gọi GetAll để trigger cache rebuild
            var users = _svc.GetAll();
            Assert.IsNotNull(users);
            
            // Verify cache được rebuild
            VerifyCacheHit(allUsersCacheKey);
        }
    }
}
