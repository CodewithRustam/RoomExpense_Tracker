using Domain.Entities;

namespace Services.ViewModels
{
    public class MonthlyExpensesDataCacheVM
    {
        public List<Expense> Expenses { get; set; } = new();
        public List<Settlement> Settlements { get; set; } = new();
    }
}
