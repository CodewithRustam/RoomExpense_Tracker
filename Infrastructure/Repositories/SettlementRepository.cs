using Domain.Entities;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Repositories
{
    public class SettlementRepository : Repository<Settlement>, ISettlementRepository
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache cache;
        public SettlementRepository(AppDbContext context, IMemoryCache _cache) : base(context)
        {
            _context = context;
            cache = _cache;
        }
        public async Task<List<Settlement>> GetMonthlySettlements(int roomId, DateTime selectedMonth)
        {
            string cacheKey = CacheHepler.GetCacheKey(roomId, selectedMonth);
            if (!cache.TryGetValue(cacheKey, out List<Settlement>? settlementDataList))
            {
                settlementDataList = await GetAllAsync(x => x.RoomId == roomId);
                cache.Set(cacheKey, settlementDataList, TimeSpan.FromDays(30));
            }
            return await GetAllAsync(x => x.RoomId == roomId);
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
    }
}
