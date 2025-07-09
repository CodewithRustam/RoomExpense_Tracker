using RoomExpenseTracker.Models;

namespace RoomExpenseTracker.ViewModels
{
    public class ExpenseSummary
    {
        public string? MemberName { get; set; }
        public decimal Total { get; set; }
        public List<Expense> Items { get; set; } = new();
    }
}
