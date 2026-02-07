namespace Services.ViewModels.ApiViewModels
{
    public class MonthlyHomeTrendVM
    {
        public string? MonthName { get; set; }
        public decimal Total { get; set; }
    }

    public class RoomTrendResponseDto
    {
        public List<MonthlyHomeTrendVM>? MonthlyTrend { get; set; }
        public decimal RoomTotal { get; set; }
    }
}
