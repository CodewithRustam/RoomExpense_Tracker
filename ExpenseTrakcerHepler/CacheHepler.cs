using Microsoft.Extensions.Caching.Memory;

namespace ExpenseTrakcerHepler
{
    public static class CacheHepler
    {
        public static string GetCacheKey(int roomId, DateTime monthDate) => $"Room_{roomId}_Month_{monthDate:yyyyMM}";

        public static string GetSettlementCacheKey(int roomId, DateTime monthDate) => $"Settlement_Room_{roomId}_Month_{monthDate:yyyyMM}";
        public static string GetMonthlyExpensesKey(int roomId, DateTime monthDate, bool isMemberInclude) => $"ExpenseDetails_Room_{roomId}_Month_{monthDate:yyyyMM}_{isMemberInclude}";
    }
}
