using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services
{
    public interface ICacheService
    {
        MemoryCacheEntryOptions CreateCacheOptions(TimeSpan? expiration = null);
        void ResetCache(string name);
        IMemoryCache GetCache();
        void SetCacheKeys(string key);
    }
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly List<string> _cacheKeys = new List<string>();

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public MemoryCacheEntryOptions CreateCacheOptions(TimeSpan? expiration = null)
        {
            return new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };
        }

        public void ResetCache(string name)
        {
            var keysToRemove = _cacheKeys.Where(k => k.StartsWith($"{name}_")).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _cacheKeys.Remove(key);
            }
        }
        public IMemoryCache GetCache()
        {
            return _cache;
        }
        public void SetCacheKeys(string key)
        {
            if (!_cacheKeys.Contains(key))
            {
                _cacheKeys.Add(key);
            }
        }
    }
}
