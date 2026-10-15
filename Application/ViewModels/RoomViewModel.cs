namespace Services.ViewModels
{
    public class RoomViewModel
    {
        [Required]
        public string Name { get; set; }

        public List<InviteMemberViewModel> Members { get; set; } = new();
    }

    public class InviteMemberViewModel
    {
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class InvitationDetailsResponse
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string InviterName { get; set; } = string.Empty;
        public string InvitedEmail { get; set; } = string.Empty;
        public string InvitedName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class AcceptInvitationRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
