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
        Task<decimal> GetMemberTotalExpenses(int roomId, int memberId, DateTime start, DateTime end);
        List<string> GetAllMonthsWithExpenses(int roomId);
    }
}
