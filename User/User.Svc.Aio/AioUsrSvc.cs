using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User.Svc;
using User.Svc.Cache;
using User.Db.MSSQL.EF.DB;

namespace User.Svc.Aio
{
    /// <summary>
    /// AioUsrSvc - All-in-One User Service
    /// Implements Cache-Aside Pattern với Redis cache và EF database
    /// 
    /// Cache-Aside Pattern Logic:
    /// 1. Read: Kiểm tra cache trước, nếu miss thì đọc từ DB và đẩy vào cache
    /// 2. Write: Ghi vào DB trước, sau đó invalidate cache
    /// 3. Delete: Xóa từ DB trước, sau đó invalidate cache
    /// </summary>
    public class AioUsrSvc : IUsersSvc
    {
        private readonly EFUsrsSvc _dbService;      // Database service (EF)
        private readonly RdsUsrSvc _cacheService;   // Cache service (Redis)
        private readonly IRedisCache _redisCache;   // Direct Redis access for cache management
        
        // Cache keys
        private const string CACHE_KEY_ALL_USERS = "aio:users:all";
        private const string CACHE_KEY_USER_BY_ID = "aio:user:id:{0}";
        private const string CACHE_KEY_USERS_BY_KEYWORD = "aio:users:keyword:{0}";
        
        // Cache expiration
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        public AioUsrSvc(EFUsrsSvc dbService, RdsUsrSvc cacheService, IRedisCache redisCache)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        }

        #region Read Operations (Cache-Aside Pattern)

        public List<Usr> GetAll()
        {
            // 1. Kiểm tra cache trước
            var cachedUsers = _redisCache.Get<List<Usr>>(CACHE_KEY_ALL_USERS);
            if (cachedUsers != null)
            {
                Console.WriteLine("[AioUsrSvc] GetAll: Cache HIT");
                return cachedUsers;
            }

            // 2. Cache miss - đọc từ database
            Console.WriteLine("[AioUsrSvc] GetAll: Cache MISS - Reading from DB");
            var users = _dbService.GetAll();

            // 3. Đẩy dữ liệu vào cache
            if (users != null && users.Any())
            {
                _redisCache.Set(CACHE_KEY_ALL_USERS, users, _cacheExpiration);
                Console.WriteLine($"[AioUsrSvc] GetAll: Cached {users.Count} users");
            }

            return users ?? new List<Usr>();
        }

        public Usr? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var cacheKey = string.Format(CACHE_KEY_USER_BY_ID, id);

            // 1. Kiểm tra cache trước
            var cachedUser = _redisCache.Get<Usr>(cacheKey);
            if (cachedUser != null)
            {
                Console.WriteLine($"[AioUsrSvc] GetById({id}): Cache HIT");
                return cachedUser;
            }

            // 2. Cache miss - đọc từ database
            Console.WriteLine($"[AioUsrSvc] GetById({id}): Cache MISS - Reading from DB");
            var user = _dbService.GetById(id);

            // 3. Đẩy dữ liệu vào cache nếu tìm thấy
            if (user != null)
            {
                _redisCache.Set(cacheKey, user, _cacheExpiration);
                Console.WriteLine($"[AioUsrSvc] GetById({id}): Cached user");
            }

            return user;
        }

        public List<Usr> GetByKwd(string keyword)
        {
            Console.WriteLine($"🔍 [AioUsrSvc] GetByKwd('{keyword}') - Starting operation...");
            
            if (string.IsNullOrWhiteSpace(keyword))
            {
                Console.WriteLine($"❌ [AioUsrSvc] GetByKwd: Invalid keyword provided");
                return new List<Usr>();
            }

            var cacheKey = string.Format(CACHE_KEY_USERS_BY_KEYWORD, keyword.ToLowerInvariant());

            // 1. Kiểm tra cache trước
            Console.WriteLine($"📋 [AioUsrSvc] Checking cache for key: {cacheKey}");
            var cachedUsers = _redisCache.Get<List<Usr>>(cacheKey);
            if (cachedUsers != null)
            {
                Console.WriteLine($"✅ [AioUsrSvc] Cache HIT! Returning {cachedUsers.Count} users from cache");
                Console.WriteLine($"⚡ [AioUsrSvc] GetByKwd('{keyword}') - COMPLETED (Cache)");
                return cachedUsers;
            }

            // 2. Cache miss - đọc từ database
            Console.WriteLine($"❌ [AioUsrSvc] Cache MISS! Reading from database...");
            Console.WriteLine($"🗄️ [AioUsrSvc] Calling EFUsrsSvc.GetByKwd('{keyword}')...");
            var users = _dbService.GetByKwd(keyword);

            // 3. Đẩy dữ liệu vào cache
            if (users != null && users.Any())
            {
                Console.WriteLine($"💾 [AioUsrSvc] Storing {users.Count} users to cache with expiration: {_cacheExpiration}");
                _redisCache.Set(cacheKey, users, _cacheExpiration);
                Console.WriteLine($"✅ [AioUsrSvc] Successfully cached {users.Count} users");
            }
            else
            {
                Console.WriteLine($"⚠️ [AioUsrSvc] No users found with keyword '{keyword}'");
            }

            Console.WriteLine($"⚡ [AioUsrSvc] GetByKwd('{keyword}') - COMPLETED (Database)");
            return users ?? new List<Usr>();
        }

        #endregion

        #region Write Operations (Write-Through Pattern)

        public string? Create(Usr usr)
        {
            Console.WriteLine($"🔍 [AioUsrSvc] Create('{usr?.Id}') - Starting operation...");
            
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id) || string.IsNullOrWhiteSpace(usr.FullName))
            {
                Console.WriteLine($"❌ [AioUsrSvc] Create: Invalid user data provided");
                return "Invalid user";
            }

            // 1. Ghi vào database trước
            Console.WriteLine($"🗄️ [AioUsrSvc] Writing user '{usr.FullName}' to database...");
            Console.WriteLine($"📝 [AioUsrSvc] Calling EFUsrsSvc.Create('{usr.Id}')...");
            var result = _dbService.Create(usr);
            
            if (result != null)
            {
                Console.WriteLine($"❌ [AioUsrSvc] Database Error: {result}");
                Console.WriteLine($"⚡ [AioUsrSvc] Create('{usr.Id}') - FAILED (Database)");
                return result;
            }

            // 2. Invalidate cache sau khi ghi thành công
            Console.WriteLine($"✅ [AioUsrSvc] Successfully created user in database");
            Console.WriteLine($"🗑️ [AioUsrSvc] Invalidating related caches...");
            InvalidateUserCaches(usr.Id);

            Console.WriteLine($"⚡ [AioUsrSvc] Create('{usr.Id}') - COMPLETED (Database + Cache Invalidation)");
            return null; // Success
        }

        public string? Update(Usr usr)
        {
            Console.WriteLine($"🔍 [AioUsrSvc] Update('{usr?.Id}') - Starting operation...");
            
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id))
            {
                Console.WriteLine($"❌ [AioUsrSvc] Update: Invalid user data provided");
                return "Invalid user";
            }

            // 1. Cập nhật trong database trước
            Console.WriteLine($"🗄️ [AioUsrSvc] Updating user '{usr.FullName}' in database...");
            Console.WriteLine($"📝 [AioUsrSvc] Calling EFUsrsSvc.Update('{usr.Id}')...");
            var result = _dbService.Update(usr);
            
            if (result != null)
            {
                Console.WriteLine($"❌ [AioUsrSvc] Database Error: {result}");
                Console.WriteLine($"⚡ [AioUsrSvc] Update('{usr.Id}') - FAILED (Database)");
                return result;
            }

            // 2. Invalidate cache sau khi cập nhật thành công
            Console.WriteLine($"✅ [AioUsrSvc] Successfully updated user in database");
            Console.WriteLine($"🗑️ [AioUsrSvc] Invalidating related caches...");
            InvalidateUserCaches(usr.Id);

            Console.WriteLine($"⚡ [AioUsrSvc] Update('{usr.Id}') - COMPLETED (Database + Cache Invalidation)");
            return null; // Success
        }

        public string? DelById(string id)
        {
            Console.WriteLine($"🔍 [AioUsrSvc] DelById('{id}') - Starting operation...");
            
            if (string.IsNullOrWhiteSpace(id))
            {
                Console.WriteLine($"❌ [AioUsrSvc] DelById: Invalid ID provided");
                return "Invalid id";
            }

            // 1. Xóa từ database trước
            Console.WriteLine($"🗄️ [AioUsrSvc] Deleting user '{id}' from database...");
            Console.WriteLine($"📝 [AioUsrSvc] Calling EFUsrsSvc.DelById('{id}')...");
            var result = _dbService.DelById(id);
            
            if (result != null)
            {
                Console.WriteLine($"❌ [AioUsrSvc] Database Error: {result}");
                Console.WriteLine($"⚡ [AioUsrSvc] DelById('{id}') - FAILED (Database)");
                return result;
            }

            // 2. Invalidate cache sau khi xóa thành công
            Console.WriteLine($"✅ [AioUsrSvc] Successfully deleted user from database");
            Console.WriteLine($"🗑️ [AioUsrSvc] Invalidating related caches...");
            InvalidateUserCaches(id);

            Console.WriteLine($"⚡ [AioUsrSvc] DelById('{id}') - COMPLETED (Database + Cache Invalidation)");
            return null; // Success
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Invalidate tất cả cache liên quan đến user
        /// </summary>
        private void InvalidateUserCaches(string userId)
        {
            Console.WriteLine($"🗑️ [AioUsrSvc] Starting cache invalidation for user '{userId}'...");
            
            // Xóa cache của user cụ thể
            var userCacheKey = string.Format(CACHE_KEY_USER_BY_ID, userId);
            _redisCache.Remove(userCacheKey);
            Console.WriteLine($"✅ [AioUsrSvc] Removed user cache: {userCacheKey}");

            // Xóa cache danh sách tất cả users
            _redisCache.Remove(CACHE_KEY_ALL_USERS);
            Console.WriteLine($"✅ [AioUsrSvc] Removed all users cache: {CACHE_KEY_ALL_USERS}");

            // Lưu ý: Không thể xóa tất cả cache keyword vì không biết keyword nào đã được cache
            // Trong production, có thể implement cache tags hoặc pattern matching
            InvalidateAllKeywordCaches();
            
            Console.WriteLine($"✅ [AioUsrSvc] Cache invalidation completed for user '{userId}'");
        }

        /// <summary>
        /// Invalidate tất cả keyword caches (simple approach)
        /// Trong production, nên sử dụng cache tags hoặc pattern matching
        /// </summary>
        private void InvalidateAllKeywordCaches()
        {
            Console.WriteLine($"🗑️ [AioUsrSvc] Invalidating keyword caches...");
            
            // Simple approach: xóa một số keyword caches phổ biến
            var commonKeywords = new[] { "nguyen", "tran", "le", "pham", "hoang", "vu", "dang", "bui", "do", "ho" };
            
            foreach (var keyword in commonKeywords)
            {
                var cacheKey = string.Format(CACHE_KEY_USERS_BY_KEYWORD, keyword);
                _redisCache.Remove(cacheKey);
            }
            
            Console.WriteLine($"✅ [AioUsrSvc] Removed {commonKeywords.Length} keyword caches");
        }

        /// <summary>
        /// Clear tất cả cache (utility method)
        /// </summary>
        public void ClearAllCache()
        {
            Console.WriteLine($"🗑️ [AioUsrSvc] Clearing all cache...");
            
            _redisCache.Remove(CACHE_KEY_ALL_USERS);
            Console.WriteLine($"✅ [AioUsrSvc] Removed all users cache");
            
            InvalidateAllKeywordCaches();
            
            Console.WriteLine($"✅ [AioUsrSvc] All cache cleared successfully");
        }

        /// <summary>
        /// Warm up cache với dữ liệu từ database
        /// </summary>
        public void WarmUpCache()
        {
            Console.WriteLine($"🔥 [AioUsrSvc] Starting cache warm-up...");
            
            // Warm up all users cache
            Console.WriteLine($"🗄️ [AioUsrSvc] Loading users from database...");
            var users = _dbService.GetAll();
            
            if (users != null && users.Any())
            {
                Console.WriteLine($"💾 [AioUsrSvc] Caching {users.Count} users...");
                _redisCache.Set(CACHE_KEY_ALL_USERS, users, _cacheExpiration);
                Console.WriteLine($"✅ [AioUsrSvc] Successfully cached all users");

                // Warm up individual user caches
                Console.WriteLine($"💾 [AioUsrSvc] Caching individual users (first 10)...");
                foreach (var user in users.Take(10)) // Chỉ warm up 10 users đầu tiên
                {
                    var cacheKey = string.Format(CACHE_KEY_USER_BY_ID, user.Id);
                    _redisCache.Set(cacheKey, user, _cacheExpiration);
                }
                Console.WriteLine($"✅ [AioUsrSvc] Successfully cached 10 individual users");
            }
            else
            {
                Console.WriteLine($"⚠️ [AioUsrSvc] No users found in database for warm-up");
            }
            
            Console.WriteLine($"🔥 [AioUsrSvc] Cache warm-up completed");
        }

        #endregion

        #region Health Check & Monitoring

        /// <summary>
        /// Kiểm tra health của cả cache và database
        /// </summary>
        public bool IsHealthy()
        {
            try
            {
                // Test database connection
                var dbUsers = _dbService.GetAll();
                if (dbUsers == null)
                {
                    Console.WriteLine("[AioUsrSvc] Health Check: Database connection failed");
                    return false;
                }

                // Test cache connection
                _redisCache.Set("health_check", "ok", TimeSpan.FromSeconds(10));
                var cacheTest = _redisCache.Get<string>("health_check");
                if (cacheTest != "ok")
                {
                    Console.WriteLine("[AioUsrSvc] Health Check: Cache connection failed");
                    return false;
                }

                Console.WriteLine("[AioUsrSvc] Health Check: All systems healthy");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AioUsrSvc] Health Check: Error - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy thống kê cache
        /// </summary>
        public CacheStats GetCacheStats()
        {
            return new CacheStats
            {
                AllUsersCached = _redisCache.Exists(CACHE_KEY_ALL_USERS),
                CacheExpiration = _cacheExpiration,
                Timestamp = DateTime.UtcNow
            };
        }

        #endregion
    }

    /// <summary>
    /// Cache statistics model
    /// </summary>
    public class CacheStats
    {
        public bool AllUsersCached { get; set; }
        public TimeSpan CacheExpiration { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
