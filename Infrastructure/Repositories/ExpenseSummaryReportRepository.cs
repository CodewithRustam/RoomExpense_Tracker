using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class ExpenseSummaryReportRepository : Repository<DailyReportLog>, IExpenseSummaryReportRepository
    {
        private readonly AppDbContext _context;

        public ExpenseSummaryReportRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public Task<List<DailyReportLog>> AddDailyReportLog(DailyReportLog dailyReportLog)
        {
            throw new NotImplementedException();
        }
    }
}
