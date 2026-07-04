namespace Services.Management
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;
        public RateLimitService(IMemoryCache cache) => _cache = cache;

        public bool IsRateLimited(string key, int limit, TimeSpan window)
        {
            if (_cache.TryGetValue(key, out int count) && count >= limit) return true;
            _cache.Set(key, count + 1, window);
            return false;
        }

        public bool ReachedDailyDeleteLimit(string userId) => _cache.TryGetValue($"RateLimit_Delete_{userId}", out int count) && count >= 2;

        public void IncrementDailyDeleteLimit(string userId)
        {
            _cache.TryGetValue($"RateLimit_Delete_{userId}", out int count);
            _cache.Set($"RateLimit_Delete_{userId}", count + 1, TimeSpan.FromDays(1));
        }
    }
}