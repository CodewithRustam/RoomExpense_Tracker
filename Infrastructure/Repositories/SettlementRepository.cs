using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SettlementRepository : Repository<Settlement>, ISettlementRepository
    {
        private readonly AppDbContext _context;

        public SettlementRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Settlement>> GetMonthlySettlements(int roomId, DateTime selectedMonth)
        {
            try
            {
               return await GetAllAsync(x => x.RoomId == roomId
                         && x.SettlementForDate.Year == selectedMonth.Year
                         && x.SettlementForDate.Month == selectedMonth.Month);
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
