namespace Services.ViewModels.ApiViewModels
{
    public class UserExpenseDetails
    {
        public List<UserExpenseResponse>? UserExpenseResponse { get; set; }
        public List<string>? Months { get; set; }
    }
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
