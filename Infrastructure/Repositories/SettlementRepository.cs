using Domain.Entities;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Dapper;

namespace Infrastructure.Repositories
{
    public class SettlementRepository : Repository<Settlement>, ISettlementRepository
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache cache;
        private readonly string _connectionString;
        public SettlementRepository(AppDbContext context, IMemoryCache _cache, IConfiguration configuration) : base(context)
        {
            _context = context;
            cache = _cache;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }
        public async Task<List<Settlement>> GetMonthlySettlements(int roomId, DateTime selectedMonth)
        {
            int year = selectedMonth.Year;
            int month = selectedMonth.Month;
            return await GetAllAsync(x => x.RoomId == roomId && x.SettlementForDate.Year == year && x.SettlementForDate.Month == month);
        }

        public async Task<List<MonthlySettlementDto>> GetMonthlySettlementsDetails(int roomId, DateTime? targetMonth = null)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new
            {
                RoomId = roomId,
                MonthDate = targetMonth ?? DateTime.Now
            };

            var result = await connection.QueryAsync<MonthlySettlementDto>(
                "GetMonthlyMemberBalances",
                param: parameters,
                commandType: System.Data.CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<List<Settlement>> GetSettlementsForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd)
        {
            var memberIds = new[] { payerMemberId, receiverMemberId };

            return await _context.Settlements
                .Where(s => s.RoomId == roomId
                            && (memberIds.Contains(s.MemberId) || memberIds.Contains(s.PaidToMemberId))
                            && s.SettlementForDate >= monthStart
                            && s.SettlementForDate <= monthEnd)
                .ToListAsync();
        }
        public async Task<bool> IsMonthSettledForRoomAsync(int roomId, DateTime month)
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            return await _context.Settlements
                .AsNoTracking()
                .AnyAsync(s => s.RoomId == roomId &&
                               s.SettlementForDate >= startOfMonth &&
                               s.SettlementForDate < endOfMonth);
        }
    }
}
