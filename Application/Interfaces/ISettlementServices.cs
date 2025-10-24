
namespace Services.Interfaces
{
    public interface ISettlementServices
    {
        Task<(bool Success, string Message)> SettleExpenseAsync(SettlementRequest settlementRequestVM);
    }
}
