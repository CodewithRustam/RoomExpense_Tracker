namespace Services.Interfaces
{
    public interface IExpenseMapper
    {
        Expense MapToExpense(ExpenseViewModel model, int memberId);

        List<ExpenseDetailResponse> MapToExpenseDetailResponses(IEnumerable<ExpenseRecordDto> expenses, string currentUserId, bool isMonthSettled);

        List<UserExpenseResponse> MapToUserExpenseResponses(IEnumerable<UserExpenseDto> expenses, string userId);
    }
}
