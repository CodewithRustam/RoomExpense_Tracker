namespace Services.ViewModels.ApiViewModels
{
    public class SettlementEmailVM
    {
        public string? PayerName { get; set; }
        public string? PayerUserName { get; set; }
        public string? PayerEmail { get; set; }

        public string? ReceiverName { get; set; }
        public string? ReceiverUserName { get; set; }
        public string? ReceiverEmail { get; set; } 

        public decimal Amount { get; set; }
        public DateTime SettlementForMonth { get; set; }

        public int RoomId { get; set; }
    }
}
