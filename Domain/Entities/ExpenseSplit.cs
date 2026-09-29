namespace Domain.Entities
{
    public class ExpenseSplit
    {
        public int ExpenseSplitId { get; set; }

        [Required]
        public int ExpenseId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal OwedAmount { get; set; }

        public decimal? Percentage { get; set; }

        public double? Shares { get; set; }

        public Expense Expense { get; set; } = null!;
        public Member Member { get; set; } = null!;
    }
}
