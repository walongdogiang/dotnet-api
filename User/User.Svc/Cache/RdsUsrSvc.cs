using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using User.Svc;

namespace User.Svc.Cache
{
    public class RdsUsrSvc : IUsersSvc
    {
        private readonly IUsersSvc _fallbackService;
        private readonly IRedisCache _redisCache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
        
        private const string CACHE_KEY_ALL_USERS = "users:all";
        private const string CACHE_KEY_USER_BY_ID = "user:id:{0}";
        private const string CACHE_KEY_USERS_BY_KEYWORD = "users:keyword:{0}";

        public RdsUsrSvc(IUsersSvc fallbackService, IRedisCache redisCache)
        {
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        }

        public List<Usr> GetAll()
        {
            // Thử lấy từ cache trước
            var cachedData = _redisCache.Get<List<Usr>>(CACHE_KEY_ALL_USERS);
            if (cachedData != null)
            {
                return cachedData;
            }

            // Nếu không có trong cache, lấy từ fallback service
            var users = _fallbackService.GetAll();
            
            // Lưu vào cache
            _redisCache.Set(CACHE_KEY_ALL_USERS, users, _cacheExpiration);
            
            return users;
        }

        public Usr? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var cacheKey = string.Format(CACHE_KEY_USER_BY_ID, id);
            
            // Thử lấy từ cache trước
            var cachedUser = _redisCache.Get<Usr>(cacheKey);
            if (cachedUser != null)
            {
                return cachedUser;
            }

            // Nếu không có trong cache, lấy từ fallback service
            var user = _fallbackService.GetById(id);
            
            // Lưu vào cache nếu tìm thấy
            if (user != null)
            {
                _redisCache.Set(cacheKey, user, _cacheExpiration);
            }
            
            return user;
        }

        public string? Create(Usr usr)
        {
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id) || string.IsNullOrWhiteSpace(usr.FullName))
                return "Invalid user";

            // Tạo user thông qua fallback service
            var result = _fallbackService.Create(usr);
            
            // Nếu tạo thành công, xóa cache liên quan
            if (result == null)
            {
                InvalidateUserCaches(usr.Id);
            }
            
            return result;
        }

        public string? Update(Usr usr)
        {
            if (usr is null || string.IsNullOrWhiteSpace(usr.Id))
                return "Invalid user";

            // Cập nhật user thông qua fallback service
            var result = _fallbackService.Update(usr);
            
            // Nếu cập nhật thành công, xóa cache liên quan
            if (result == null)
            {
                InvalidateUserCaches(usr.Id);
            }
            
            return result;
        }

        public string? DelById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "Invalid id";

            // Xóa user thông qua fallback service
            var result = _fallbackService.DelById(id);
            
            // Nếu xóa thành công, xóa cache liên quan
            if (result == null)
            {
                InvalidateUserCaches(id);
            }
            
            return result;
        }

        public List<Usr> GetByKwd(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<Usr>();

            var cacheKey = string.Format(CACHE_KEY_USERS_BY_KEYWORD, keyword.ToLowerInvariant());
            
            // Thử lấy từ cache trước
            var cachedUsers = _redisCache.Get<List<Usr>>(cacheKey);
            if (cachedUsers != null)
            {
                return cachedUsers;
            }

            // Nếu không có trong cache, lấy từ fallback service
            var users = _fallbackService.GetByKwd(keyword);
            
            // Lưu vào cache
            _redisCache.Set(cacheKey, users, _cacheExpiration);
            
            return users;
        }

        private void InvalidateUserCaches(string userId)
        {
            // Xóa cache của user cụ thể
            var userCacheKey = string.Format(CACHE_KEY_USER_BY_ID, userId);
            _redisCache.Remove(userCacheKey);
            
            // Xóa cache danh sách tất cả users
            _redisCache.Remove(CACHE_KEY_ALL_USERS);
            
            // Lưu ý: Không thể xóa tất cả cache keyword vì không biết keyword nào đã được cache
            // Có thể implement pattern để track các keyword đã cache hoặc sử dụng cache tags
        }
    }

    // Interface cho Redis cache operations
    public interface IRedisCache
    {
        T? Get<T>(string key) where T : class;
        void Set<T>(string key, T value, TimeSpan expiration) where T : class;
        void Remove(string key);
        bool Exists(string key);
    }

    // Mock implementation cho Redis cache (có thể thay thế bằng StackExchange.Redis)
    public class MockRedisCache : IRedisCache
    {
        private readonly Dictionary<string, (object value, DateTime expiration)> _cache = new();

        public T? Get<T>(string key) where T : class
        {
            if (!_cache.TryGetValue(key, out var item))
                return null;

            if (DateTime.UtcNow > item.expiration)
            {
                _cache.Remove(key);
                return null;
            }

            return item.value as T;
        }

        public void Set<T>(string key, T value, TimeSpan expiration) where T : class
        {
            _cache[key] = (value, DateTime.UtcNow.Add(expiration));
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public bool Exists(string key)
        {
            return _cache.ContainsKey(key) && DateTime.UtcNow <= _cache[key].expiration;
        }
    }
}