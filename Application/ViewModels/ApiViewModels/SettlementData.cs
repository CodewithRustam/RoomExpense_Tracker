namespace Services.ViewModels.ApiViewModels
{
    public class SettlementData
    {
        public decimal NetBalance { get; set; }
        public List<SettlementDetail> Settlements { get; set; } = new List<SettlementDetail>();
    }

    public class SettlementDetail
    {
        public int ToMemberId { get; set; }
        public string ToMemberName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
