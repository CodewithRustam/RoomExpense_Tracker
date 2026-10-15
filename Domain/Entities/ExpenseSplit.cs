using ExpenseTrakcerHepler;

namespace Domain.Entities
{
    public class ExpenseSplit : IAuditableEntity
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

        public DateTime CreatedDate { get; set; } = DateTimeProvider.NowIST;
        public string? CreatedByUserId { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedByUserId { get; set; }
    }
}
