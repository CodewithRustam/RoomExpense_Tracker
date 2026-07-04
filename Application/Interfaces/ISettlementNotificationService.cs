namespace Services.Interfaces
{
    public interface ISettlementNotificationService
    {
        void FireAndForgetSettlementEmail(int roomId, string payerId, string payerName, string receiverId, string receiverName, decimal amount, DateTime settlementMonth);
    }
}
