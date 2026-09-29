namespace Services.Management
{
    public class ExpenseMapper : IExpenseMapper
    {
        public Expense MapToExpense(ExpenseViewModel model, int memberId)
        {
            if (model == null) return new Expense();

            var expense = new Expense
            {
                ExpenseId = model.ExpenseId ?? 0,
                MemberId = memberId,
                RoomId = model.RoomId,
                Item = model.Item?.Trim(),
                Amount = model.Amount,
                Date = model.Date.Date,
                Category = CategoryMapper.GetCategoryFromItem(model.Item ?? string.Empty),
                SplitType = (SplitType)model.SplitType
            };

            if (model.Splits != null && model.Splits.Count > 0)
            {
                foreach (var split in model.Splits)
                {
                    expense.ExpenseSplits.Add(new ExpenseSplit
                    {
                        MemberId = split.MemberId,
                        OwedAmount = Math.Round(split.OwedAmount, 2),
                        Percentage = split.Percentage,
                        Shares = split.Shares
                    });
                }
            }

            return expense;
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
                IsEditShow = e.ApplicationUserId == currentUserId && !isMonthSettled,
                SplitType = (int)e.SplitType,
                Splits = e.Splits?.Select(s => new ExpenseSplitDto
                {
                    MemberId = s.MemberId,
                    MemberName = s.MemberName,
                    OwedAmount = s.OwedAmount,
                    Percentage = s.Percentage,
                    Shares = s.Shares
                }).ToList() ?? new List<ExpenseSplitDto>()
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