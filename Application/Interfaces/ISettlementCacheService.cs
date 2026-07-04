namespace Services.Interfaces
{
    public interface ISettlementCacheService
    {
        void ClearCachesAfterSettlement(int roomId, int payerId, int receiverId, string userId, DateTime settlementMonth);
    }
}
