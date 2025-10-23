using Services.ViewModels;
using Services.ViewModels.ApiViewModels;

namespace Services.Interfaces
{
    public interface IExpenseServices
    {
        Task<ApiResponse> AddExpenses(ExpenseViewModel expenseViewModel);
        Task<RoomExpensesViewModel> GetMonthlyExpenses(int roomId, DateTime selectedMonth);
        Task<ApiResponse> UpdateExpenses(ExpenseViewModel expenseViewModel);
        Task<ApiResponse> GetRoomExpensesForApi(int roomId, DateTime selectedMonth, bool includeRoomInfo = true);
        Task<ApiResponse> GetUserExpensesForApi(DateTime month);
        Task<ApiResponse> GetMonthlyExpensesTrend(int roomId, string month);
        Task<SettlementData> GetSettlementDetails(int roomId, int memberId, DateTime? month = null);
        Task<List<string>?> GetExpenseMonths(int roomId);
    }
}
