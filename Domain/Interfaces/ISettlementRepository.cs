namespace Domain.Interfaces
{
    public interface ISettlementRepository : IRepository<Settlement>
    {
        Task<List<Settlement>> GetMonthlySettlements(int roomId, DateTime selectedMonth);
        Task<List<MonthlySettlementDto>> GetMonthlySettlementsDetails(int roomId, DateTime? targetMonth);
        Task<List<Settlement>> GetSettlementsForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd);
        Task<bool> IsMonthSettledForRoomAsync(int roomId, DateTime month);
    }
}
