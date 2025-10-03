namespace Services.ViewModels.ApiViewModels
{
    public class RoomExpenseResponse
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public List<MemberData> MembersData { get; set; } = null!;
        public List<string> AvailableMonths { get; set; } = new();
        public string SelectedMonth { get; set; } = null!;
        public decimal TotalExpense { get; set; }
        public List<ExpenseDetailResponse> Expenses { get; set; } = new();
    }
    public class ExpenseDetailResponse
    {
        public int ExpenseId { get; set; }
        public int RoomId { get; set; }
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string PayerName { get; set; } = null!;
        public int PayerId { get; set; }
        public string Category { get; set; } = null!;
        public string IconName { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
    public class MemberData
    {
        public string? MemberName { get; set; }
        public int MemberId { get; set; }
    }
}
