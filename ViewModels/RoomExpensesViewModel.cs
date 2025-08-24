using RoomExpenseTracker.Models;

namespace RoomExpenseTracker.ViewModels
{
    public class RoomExpensesViewModel
    {
        public List<ExpenseSummary> Summary { get; set; } = new();
        public decimal? TotalExpense { get; set; }
        public decimal? AvgPerPerson { get; set; }
        public Expense? Expense { get; set; } = new Expense();
        public bool IsTwoMembersInRoom { get; set; }
    }
}
