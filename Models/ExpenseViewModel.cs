using Microsoft.AspNetCore.Mvc.Rendering;

namespace RoomExpenseTracker.Models
{
    public class ExpenseViewModel
    {
        public Expense Expense { get; set; }
        public int RoomId { get; set; }
    }
}
