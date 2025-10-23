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
        Task<ApiResponse> GetUserExpensesForApi(string month);
        Task<ApiResponse> GetMonthlyExpensesTrend(int roomId, string month);
        Task<ApiResponse> GetSettlementDetails(int roomId, int memberId, string month);
        Task<List<string>?> GetExpenseMonthsByUserId();
    }
}
