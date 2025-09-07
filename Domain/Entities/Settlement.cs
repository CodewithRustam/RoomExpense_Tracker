using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Settlement
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
    }
}
