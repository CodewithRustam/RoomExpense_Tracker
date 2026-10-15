using ExpenseTrakcerHepler;

namespace Domain.Entities
{
    public class Settlement : IAuditableEntity
    {
        public int SettlementId { get; set; }
        [Required]
        public int MemberId { get; set; }
        [Required]
        public int RoomId { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Settlement amount must be greater than zero")]
        public decimal Amount { get; set; }
        public DateTime SettlementDate { get; set; }
        public DateTime SettlementForDate { get; set; }
        public int PaidToMemberId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTimeProvider.NowIST;
        public string? CreatedByUserId { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedByUserId { get; set; }
    }
}
