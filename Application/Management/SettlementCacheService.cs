namespace Services.Management
{
    public class SettlementCacheService : ISettlementCacheService
    {
        private readonly IMemoryCache _cache;

        public SettlementCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void ClearCachesAfterSettlement(int roomId, int payerId, int receiverId, string userId, DateTime settlementMonth)
        {
            _cache.Remove(CacheHelper.GetCacheKey(roomId, settlementMonth));
            _cache.Remove(CacheHelper.GetRoomsUserKey(userId));
            _cache.Remove(CacheHelper.GetMonthlyExpenseTrendKey(roomId, settlementMonth));
            _cache.Remove(CacheHelper.GetUserExpensesKey(userId, settlementMonth));
            _cache.Remove(CacheHelper.GetSettlementCacheKey(roomId, payerId, settlementMonth));
            _cache.Remove(CacheHelper.GetSettlementCacheKey(roomId, receiverId, settlementMonth));

            foreach (var isMemberInclude in new[] { true, false })
            {
                _cache.Remove(CacheHelper.GetMonthlyExpensesKey(roomId, settlementMonth, isMemberInclude));
            }
        }
    }
}
