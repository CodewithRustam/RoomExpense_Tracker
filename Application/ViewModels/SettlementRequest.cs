namespace Services.ViewModels
{
    public class SettlementRequest
    {
        public int RoomId { get; set; }
        public string PayerName { get; set; } = string.Empty;       
        public string ReceiverName { get; set; } = string.Empty;       
        public decimal SettlementAmount { get; set; }                 
        public string MonthLabel { get; set; } = string.Empty;
        public DateTime? SettlementMonth { get; set; } = null;              
    }

}
