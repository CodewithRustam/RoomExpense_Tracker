namespace Services.ViewModels.ApiViewModels
{
    public class MonthlyExpensesTrendResponse
    {
        public List<string> Months { get; set; } = new(); 
        public List<MemberExpenses> Members { get; set; } = new();
    }

    public class MemberExpenses
    {
        public string Name { get; set; } = string.Empty; 
        public List<decimal> MonthlyExpenses { get; set; } = new();
        public List<decimal>? Settled { get; set; } = null;
    }
}
