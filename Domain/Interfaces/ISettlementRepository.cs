using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ISettlementRepository : IRepository<Settlement>
    {
        Task<List<Settlement>> GetMonthlySettlements(int roomId, DateTime selectedMonth);
        Task<List<Settlement>> GetSettlementsForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd);
    }
}
