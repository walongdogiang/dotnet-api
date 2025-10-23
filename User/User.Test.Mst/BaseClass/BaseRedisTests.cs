using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using User.Svc;
using User.Svc.Cache;

namespace User.Test.Mst.BaseClass
{
    public abstract class BaseRedisTests<TSvc> where TSvc : class, IUsersSvc
    {
        protected IUsersSvc _svc;
        protected IRedisCache _redisCache;
        protected IUsersSvc _fallbackService;
        
        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();

            // Tạo fallback service (UsersSvc)
            services.AddSingleton<ITimeProvider, SystemTimeProvider>()
                    .AddSingleton<IUsersSvc, UsersSvc>();

            // Tạo Redis cache mock
            services.AddSingleton<IRedisCache, MockRedisCache>();

            // Tạo RdsUsrSvc với fallback service
            services.AddSingleton<RdsUsrSvc>();

            var provider = services.BuildServiceProvider();
            _svc = provider.GetRequiredService<TSvc>();
            _redisCache = provider.GetRequiredService<IRedisCache>();
            _fallbackService = provider.GetRequiredService<UsersSvc>();
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
                "users:all",
                "user:id:1", "user:id:2", "user:id:3",
                "users:keyword:nguyen", "users:keyword:tran", "users:keyword:le"
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
    }
}
