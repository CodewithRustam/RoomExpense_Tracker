namespace Services.ViewModels
{
    public class ExpenseSplitDto
    {
        public int MemberId { get; set; }
        public string? MemberName { get; set; }
        public decimal OwedAmount { get; set; }
        public decimal? Percentage { get; set; }
        public double? Shares { get; set; }
    }
}
