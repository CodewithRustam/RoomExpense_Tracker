using Domain.Entities;
using Domain.Entities.Dto;

namespace Domain.Interfaces
{
    public interface IExpenseRepository: IRepository<Expense>
    {
        Task<string> AddExpenses(Expense expense);
        Task<List<Expense>> GetMonthlyExpenses(int roomId, DateTime selectedMonth);
        Task<bool> IsExpenseExist(Expense expense);
        Task<(bool IsUpdated, string Message)> UpdateExpenses(Expense expense);
        Task<decimal> GetTotalRoomExpenses(int roomId, DateTime start, DateTime end);
        Task<List<UserExpenseDto>> GetUserExpenses(string userId, DateTime month);
        List<string?> GetDeviceToken(int roomId);
        Task<List<Expense>> GetExpensesForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd);
        Task<(List<MemberExpensesDto> Members, List<CategoryExpenseDto> Categories, List<CategoryExpenseDto> TopSpends)> GetMonthlyExpensesTrendAsync(int roomId, DateTime targetMonth);
        Task<List<string>> GetExpenseMonths();
    }
}
