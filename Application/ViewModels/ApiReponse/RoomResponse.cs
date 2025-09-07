namespace Services.ViewModels.ApiReponse
{
    public class RoomResponse
    {
        public int RoomId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<MemberResponse> Members { get; set; } = new();
    }

    public class MemberResponse
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
