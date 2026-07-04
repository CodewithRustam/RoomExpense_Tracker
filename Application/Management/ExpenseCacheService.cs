namespace Services.Management
{
    public class ExpenseCacheService : IExpenseCacheService
    {
        private readonly IMemoryCache _cache;

        public ExpenseCacheService(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public void ClearCachesOnAddOrUpdate(int roomId, DateTime date, string userId, int memberId)
        {
            _cache.Remove(CacheHelper.GetCacheKey(roomId, date));
            _cache.Remove(CacheHelper.GetRoomsUserKey(userId));
            _cache.Remove(CacheHelper.GetUserExpensesKey(userId, date));
            _cache.Remove(CacheHelper.GetMonthlyExpenseTrendKey(roomId, date));
            _cache.Remove(CacheHelper.GetSettlementCacheKey(roomId, memberId, date));
            _cache.Remove(CacheHelper.GetMonthlyExpensesKey(roomId, date, true));
            _cache.Remove(CacheHelper.GetMonthlyExpensesKey(roomId, date, false));
        }

        public void ClearCachesOnDelete(int roomId, DateTime date, string userId)
        {
            _cache.Remove(CacheHelper.GetCacheKey(roomId, date));
            _cache.Remove(CacheHelper.GetRoomsUserKey(userId));
            _cache.Remove(CacheHelper.GetUserExpensesKey(userId, date));
            _cache.Remove(CacheHelper.GetMonthlyExpenseTrendKey(roomId, date));
        }

        public bool TryGetMonthlyTrend(int roomId, DateTime targetMonth, out MonthlyExpensesTrendResponse? response)
            => _cache.TryGetValue(CacheHelper.GetMonthlyExpenseTrendKey(roomId, targetMonth), out response);

        public void SetMonthlyTrend(int roomId, DateTime targetMonth, MonthlyExpensesTrendResponse response)
            => _cache.Set(CacheHelper.GetMonthlyExpenseTrendKey(roomId, targetMonth), response, TimeSpan.FromDays(30));

        public bool TryGetUserExpenses(string userId, DateTime targetMonth, out List<UserExpenseResponse>? response)
            => _cache.TryGetValue(CacheHelper.GetUserExpensesKey(userId, targetMonth), out response);

        public void SetUserExpenses(string userId, DateTime targetMonth, List<UserExpenseResponse> response)
            => _cache.Set(CacheHelper.GetUserExpensesKey(userId, targetMonth), response, TimeSpan.FromDays(30));
    }
}