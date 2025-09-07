using Domain.Entities;

namespace Services.ViewModels.ApiViewModels
{
    public class RoomExpensesViewModel
    {
        public List<ExpenseSummary> Summary { get; set; } = new List<ExpenseSummary>();
        public decimal TotalExpense { get; set; }
        public decimal AvgPerPerson { get; set; }
        public bool IsTwoMembersInRoom { get; set; }
        public Expense Expense { get; set; } = new Expense();
    }
}
