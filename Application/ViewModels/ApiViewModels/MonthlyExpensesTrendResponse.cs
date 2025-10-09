namespace Services.ViewModels.ApiViewModels
{
    public class MonthlyExpensesTrendResponse
    {
        public List<string> Months { get; set; } = new();
        public List<MemberExpenses> Members { get; set; } = new();
        public List<CategoryMonthlyExpense> CategoryExpenses { get; set; } = new();
        public List<TopSpend> TopSpends { get; set; } = new();
    }

    public class MemberExpenses
    {
        public string Name { get; set; } = string.Empty;
        public List<decimal> MonthlyExpenses { get; set; } = new();
    }

    public class CategoryMonthlyExpense
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<decimal> MonthlyTotals { get; set; } = new();
        public string IconName { get; set; } = string.Empty; // Added icon name
    }

    public class TopSpend
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<decimal> MonthlyTotals { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public string IconName { get; set; } = string.Empty; // Added icon name
    }
}