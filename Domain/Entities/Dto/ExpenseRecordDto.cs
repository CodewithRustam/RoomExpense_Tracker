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
    }

}
