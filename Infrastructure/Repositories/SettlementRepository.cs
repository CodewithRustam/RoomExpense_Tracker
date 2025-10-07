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
            try
            {
                string cacheKey = CacheHepler.GetCacheKey(roomId, selectedMonth);
                if (!cache.TryGetValue(cacheKey, out List<Settlement>? settlementDataList))
                {
                    settlementDataList = await GetAllAsync(x => x.RoomId == roomId);
                    cache.Set(cacheKey, settlementDataList, TimeSpan.FromDays(30));
                }

            return await GetAllAsync(x => x.RoomId == roomId);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task AddSettlement(Settlement settlement)
        {
            try
            {
                await AddAsync(settlement);
                await SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<decimal> GetSettlementsPaid(int roomId, int memberId, DateTime start, DateTime end)
        {
            try
            {
                 return await _context.Settlements.Where(s => s.RoomId == roomId && s.MemberId == memberId && s.SettlementForDate >= start && s.SettlementForDate <= end).SumAsync(s => s.Amount);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<decimal> GetSettlementsReceived(int roomId, int memberId, DateTime start, DateTime end)
        {
            try
            {
                return await _context.Settlements.Where(s => s.RoomId == roomId && s.PaidToMemberId == memberId && s.SettlementForDate >= start && s.SettlementForDate <= end).SumAsync(s => s.Amount);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
