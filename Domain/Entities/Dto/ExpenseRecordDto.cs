namespace Domain.Entities.Dto
{
    public class ExpenseRecordDto
    {
        public string ApplicationUserId { get; set; } = null!;
        public string PayerName { get; set; } = null!;
        public int PayerId { get; set; }
        public int ExpenseId { get; set; }
        public int RoomId { get; set; }
        public string Item { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; } = null!;
        public SplitType SplitType { get; set; }
        public List<ExpenseSplitRecordDto> Splits { get; set; } = new();
    }

    public class ExpenseSplitRecordDto
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public decimal OwedAmount { get; set; }
        public decimal? Percentage { get; set; }
        public double? Shares { get; set; }
    }
}
