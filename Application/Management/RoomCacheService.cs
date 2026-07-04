namespace Services.Management
{
    public class RoomCacheService : IRoomCacheService
    {
        private readonly IMemoryCache _cache;

        public RoomCacheService(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public bool TryGetRooms(string userId, out IEnumerable<RoomResponse>? rooms)
        {
            string cacheKey = CacheHelper.GetRoomsUserKey(userId);
            return _cache.TryGetValue(cacheKey, out rooms);
        }

        public void SetRooms(string userId, IEnumerable<RoomResponse> rooms)
        {
            string cacheKey = CacheHelper.GetRoomsUserKey(userId);
            _cache.Set(cacheKey, rooms, TimeSpan.FromDays(30));
        }

        public void ClearRoomsCache(string userId)
        {
            string cacheKey = CacheHelper.GetRoomsUserKey(userId);
            _cache.Remove(cacheKey);
        }
    }
}