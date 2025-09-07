
namespace Services.Interfaces
{
    public interface ISettlementServices
    {
        Task<(bool Success, string Message)> SettleExpenseAsync(int roomId, string memberName, string paidToMemberName, decimal amount, DateTime settlementForMonth);
    }
}
