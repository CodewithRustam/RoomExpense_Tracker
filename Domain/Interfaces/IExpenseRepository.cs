using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IExpenseRepository : IRepository<Expense>
    {
        Task<IReadOnlyList<ExpenseRecordDto>> GetMonthlyExpenses(int roomId, DateTime selectedMonth);

        Task<IReadOnlyList<UserExpenseDto>> GetUserExpenses(string userId, DateTime month);

        Task<IReadOnlyList<Expense>> GetExpensesForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd);

        Task<IReadOnlyList<Expense>> GetHomeExpenseTrends(int roomId, DateTime startDate);

        Task<IReadOnlyList<string>> GetExpenseMonths(int roomId);

        Task<IReadOnlyList<string>> GetExpenseMonthsByUserId(string userId);

        Task<(IReadOnlyList<MemberExpensesDto> Members, IReadOnlyList<CategoryExpenseDto> Categories, IReadOnlyList<CategoryExpenseDto> TopSpends)> GetMonthlyExpensesTrendAsync(int roomId, DateTime targetMonth);

        Task<decimal> GetTotalRoomExpenses(int roomId, DateTime start, DateTime end);

        Task<bool> IsExpenseExist(Expense expense);

        Task<bool> IsExpenseExistForUser(Expense expense);

    }
}