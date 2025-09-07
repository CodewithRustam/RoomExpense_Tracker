using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IExpenseSummaryReportRepository : IRepository<DailyReportLog>
    {
        public Task<List<DailyReportLog>> AddDailyReportLog(DailyReportLog dailyReportLog);
    }
}
