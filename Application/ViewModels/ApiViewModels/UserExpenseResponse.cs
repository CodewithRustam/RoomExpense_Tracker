namespace Services.ViewModels.ApiViewModels
{
    public class UserExpenseResponse
    {
        public string? Item { get; set; }
        public string? RoomName { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? IconName { get; set; }
        public string? UserId { get; set; }
    }
}
