namespace Services.Interfaces
{
    public interface IExpenseCacheService
    {
        void ClearCachesOnAddOrUpdate(int roomId, DateTime date, string userId, int memberId);
        void ClearCachesOnDelete(int roomId, DateTime date, string userId);
        bool TryGetMonthlyTrend(int roomId, DateTime targetMonth, out MonthlyExpensesTrendResponse? response);
        void SetMonthlyTrend(int roomId, DateTime targetMonth, MonthlyExpensesTrendResponse response);
        bool TryGetUserExpenses(string userId, DateTime targetMonth, out List<UserExpenseResponse>? response);
        void SetUserExpenses(string userId, DateTime targetMonth, List<UserExpenseResponse> response);
    }
}