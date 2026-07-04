using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface ISettlementRepository : IRepository<Settlement>
    {
        Task<IReadOnlyList<Settlement>> GetMonthlySettlements(int roomId, DateTime selectedMonth);

        Task<IReadOnlyList<MonthlySettlementDto>> GetMonthlySettlementsDetails(int roomId, DateTime? targetMonth = null);

        Task<IReadOnlyList<Settlement>> GetSettlementsForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd);

        Task<bool> IsMonthSettledForRoomAsync(int roomId, DateTime month);
    }
}