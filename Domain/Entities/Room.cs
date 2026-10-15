using ExpenseTrakcerHepler;

namespace Domain.Entities
{
    public class Room : IAuditableEntity
    {
        public int RoomId { get; set; }
        [Required]
        public string? Name { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTimeProvider.NowIST;
        public ApplicationUser? CreatedByUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedByUserId { get; set; }
        public ApplicationUser? UpdatedByUser { get; set; }
        public bool IsDeleted { get; set; }
        public List<Member> Members { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();
        public List<RoomInvitation> Invitations { get; set; } = new();
    }
}
