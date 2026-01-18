namespace Services.ViewModels
{
    public class ExpenseViewModel
    {
        public int RoomId { get; set; }
        public int? ExpenseId { get; set; }
        public int MemberId { get; set; }
        [Required]
        public string? Item { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
