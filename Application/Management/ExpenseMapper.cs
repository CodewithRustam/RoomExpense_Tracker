namespace Services.Management
{
    public class ExpenseMapper : IExpenseMapper
    {
        public Expense MapToExpense(ExpenseViewModel model, int memberId)
        {
            if (model == null) return new Expense();

            return new Expense
            {
                ExpenseId = model.ExpenseId ?? 0,
                MemberId = memberId,
                RoomId = model.RoomId,
                Item = model.Item?.Trim(),
                Amount = model.Amount,
                Date = model.Date.Date,
                Category = CategoryMapper.GetCategoryFromItem(model.Item ?? string.Empty)
            };
        }

        public List<ExpenseDetailResponse> MapToExpenseDetailResponses(IEnumerable<ExpenseRecordDto> expenses, string currentUserId, bool isMonthSettled)
        {
            if (expenses == null || !expenses.Any()) return new List<ExpenseDetailResponse>();

            return expenses.Select(e => new ExpenseDetailResponse
            {
                ExpenseId = e.ExpenseId,
                RoomId = e.RoomId,
                Item = e.Item ?? string.Empty,
                Amount = e.Amount,
                Date = e.Date,
                PayerName = e.PayerName,
                PayerId = e.PayerId,
                Category = e.Category ?? string.Empty,
                IconName = CategoryMapper.GetIconForCategory(e.Category ?? string.Empty),
                IsEditShow = e.ApplicationUserId == currentUserId && !isMonthSettled
            }).ToList();
        }

        public List<UserExpenseResponse> MapToUserExpenseResponses(IEnumerable<UserExpenseDto> expenses, string userId)
        {
            if (expenses == null || !expenses.Any()) return new List<UserExpenseResponse>();

            return expenses.Select(e => new UserExpenseResponse
            {
                Item = e.Item ?? string.Empty,
                RoomName = e.RoomName ?? string.Empty,
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                IconName = CategoryMapper.GetIconForCategory(e.Category ?? string.Empty),
                UserId = userId
            }).ToList();
        }
    }
}