namespace Services.ViewModels.ApiReponse
{
    public class RoomResponse
    {
        public int RoomId { get; set; }
        public string? Name { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? MemberNames { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Type { get; set; }
        public string? IconName { get; set; }
        public string? Status { get; set; }
        public string? Month { get; set; }
    }
}
