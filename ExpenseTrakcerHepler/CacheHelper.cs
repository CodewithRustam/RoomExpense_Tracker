namespace ExpenseTrakcerHepler
{
    public static class CacheHelper
    {
        public static string GetCacheKey(int roomId, DateTime monthDate) => $"Room_{roomId}_Month_{monthDate:yyyyMM}";
        public static string GetSettlementCacheKey(int roomId,int memberId, DateTime month) => $"SettlementDetails_roomId_{roomId}_memId_{memberId}_{month:yyyyMM}";
        public static string GetMonthlyExpensesKey(int roomId, DateTime monthDate, bool isMemberInclude) => $"ExpenseDetails_Room_{roomId}_Month_{monthDate:yyyyMM}_{isMemberInclude}";
        public static string GetRoomsUserKey(string? userId) => $"RoomsForUser_{userId}";
        public static string GetUserExpensesKey(string? userId, DateTime targetMonth) => $"UserExpenses_{userId}_{targetMonth:yyyyMM}";
        public static string GetMonthlyExpenseTrendKey(int roomId, DateTime targetMonth) => $"MonthlyExpensesTrend_Room_{roomId}_{targetMonth:yyyyMM}";
    }
}
