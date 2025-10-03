using Services.ViewModels;
using Services.ViewModels.ApiViewModels;

namespace Services.Interfaces
{
    public interface IExpenseServices
    {
        Task<string> AddExpenses(ExpenseViewModel viewModel);
        Task<RoomExpensesViewModel> GetMonthlyExpenses(int roomId, DateTime selectedMonth);
        Task<string> UpdateExpenses(ExpenseViewModel viewModel);
        Task<RoomExpenseResponse> GetRoomExpensesForApi(int roomId, DateTime selectedMonth, bool includeRoomInfo = true);
        Task<List<UserExpenseResponse>> GetUserExpensesForApi();
    }
}
