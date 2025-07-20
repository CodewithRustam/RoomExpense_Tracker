using RoomExpenseTracker.Models;

namespace RoomExpenseTracker.ViewModels
{
    public class ExpenseSummary
    {
        public string? MemberName { get; set; }
        public decimal? TotalExpense { get; set; }
        public decimal? NetBalance { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? ReceivedAmount { get; set; }
        public List<Expense> Items { get; set; } = new();
    }
}
