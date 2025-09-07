using Domain.Entities;

namespace Services.ViewModels
{
    public class ExpenseViewModel
    {
        public Expense? Expense { get; set; }
        public int RoomId { get; set; }
    }
}
