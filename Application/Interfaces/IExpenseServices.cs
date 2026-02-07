namespace Services.Interfaces
{
    public interface IExpenseServices
    {
        Task<ApiResponse> AddExpenses(ExpenseViewModel expenseViewModel);
        Task<ApiResponse> UpdateExpenses(ExpenseViewModel expenseViewModel);
        Task<ApiResponse> GetRoomExpensesForApi(int roomId, string? selectedMonth, bool includeRoomInfo = true);
        Task<ApiResponse> GetUserExpensesForApi(string month);
        Task<ApiResponse> GetMonthlyExpensesTrend(int roomId, string month);
        Task<ApiResponse> GetSettlementDetails(int roomId, int memberId, string month);
        Task<List<string>?> GetExpenseMonthsByUserId();
        Task<ApiResponse> DeleteExpense(int expenseId);
        Task<ApiResponse> GetHomeExpenseTrends(int roomId);
    }
}
