using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Expense
    {
        public int ExpenseId { get; set; }
        [Required]
        public int MemberId { get; set; }
        [Required]
        public string? Item { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        [Required]
        public int RoomId { get; set; }
        public bool? IsDeleted { get; set; }
        public bool IsNonSplitExpense { get; set; }
        public int? OwedToMemberId { get; set; }
        public int? OweToMemberId { get; set; }
        public Room Room { get; set; }
        public Member Member { get; set; }
    }
}