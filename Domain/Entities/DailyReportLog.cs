namespace Domain.Entities
{
    public class DailyReportLog
    {
        public int DailyReportLogId { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public DateTime RunDate { get; set; }
        public string? ReportName { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
