namespace Domain.Interfaces
{
    public interface IExpenseRepository: IRepository<Expense>
    {
        Task<List<ExpenseRecordDto>> GetMonthlyExpenses(int roomId, DateTime selectedMonth);
        Task<bool> IsExpenseExist(Expense expense);
        Task<decimal> GetTotalRoomExpenses(int roomId, DateTime start, DateTime end);
        Task<List<UserExpenseDto>> GetUserExpenses(string userId, DateTime month);
        List<string?> GetDeviceToken(int roomId, string? userId);
        Task<List<Expense>> GetExpensesForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd);
        Task<(List<MemberExpensesDto> Members, List<CategoryExpenseDto> Categories, List<CategoryExpenseDto> TopSpends)> GetMonthlyExpensesTrendAsync(int roomId, DateTime targetMonth);
        Task<List<string>> GetExpenseMonths(int roomId);
        Task<List<string>> GetExpenseMonthsByUserId(string userId);
        Task<bool> IsExpenseExistForUser(Expense expense);
        Task<List<Expense>> GetHomeExpenseTrends(int roomId, DateTime startDate);
    }
}
