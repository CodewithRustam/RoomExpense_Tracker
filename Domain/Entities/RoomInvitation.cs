namespace Domain.Entities
{
    public class RoomInvitation : BaseAuditableEntity
    {
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string InvitedEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string InvitedName { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string Token { get; set; } = string.Empty;

        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        public DateTime ExpiresAt { get; set; }

        public DateTime? AcceptedAt { get; set; }
    }
}
