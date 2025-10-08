using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IExpenseRepository: IRepository<Expense>
    {
        Task<string> AddExpenses(Expense expense);
        Task<List<Expense>> GetMonthlyExpenses(int roomId, DateTime selectedMonth);
        Task<bool> IsExpenseExist(Expense expense);
        Task<(bool IsUpdated, string Message)> UpdateExpenses(Expense expense);
        Task<decimal> GetTotalRoomExpenses(int roomId, DateTime start, DateTime end);
        Task<List<Expense>> GetUserExpenses(string userId, DateTime startDate, DateTime endDate);
        List<string?> GetDeviceToken(int roomId);
        Task<List<Expense>> GetExpensesForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd);
    }
}
