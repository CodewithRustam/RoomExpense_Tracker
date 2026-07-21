namespace Services.ViewModels.ApiViewModels
{
    public class RoomExpenseResponse
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public List<string> AvailableMonths { get; set; } = new();
        public string SelectedMonth { get; set; } = null!;
        public decimal TotalMontlyExpense { get; set; }
        public string? CreatedByUserId { get; set; }
        public List<MemberExpenseSummary> MembersSummary { get; set; } = new();
        public List<ExpenseDetailResponse> Expenses { get; set; } = new();
    }

    public class ExpenseDetailResponse
    {
        public int ExpenseId { get; set; }
        public int RoomId { get; set; }
        public string Item { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string PayerName { get; set; } = null!;
        public int PayerId { get; set; }
        public string Category { get; set; } = null!;
        public string IconName { get; set; } = null!;
        public bool IsEditShow { get; set; }
    }
    public class MemberExpenseSummary
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public decimal TotalMemberExpense { get; set; }
        public decimal AmountReceived { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal NetBalance { get; set; }
        public string BadgeText { get; set; } = string.Empty;
        public decimal BadgeAmount { get; set; }
        public bool IsSettleShow { get; set; }
    }
}
