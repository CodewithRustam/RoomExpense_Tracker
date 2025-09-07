namespace Services.ViewModels
{
    public class SettlementRequest
    {
        public int RoomId { get; set; }
        public string MemberName { get; set; } = "";
        public string PaidToMemberName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Month { get; set; } = "";
    }
}
