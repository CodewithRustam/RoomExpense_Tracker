using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ISettlementRepository : IRepository<Settlement>
    {
        Task AddSettlement(Settlement settlement);
        Task<List<Settlement>> GetMonthlySettlements(int roomId, DateTime selectedMonth);
        Task<decimal> GetSettlementsPaid(int roomId, int memberId, DateTime start, DateTime end);
        Task<decimal> GetSettlementsReceived(int roomId, int memberId, DateTime start, DateTime end);
    }
}
