using Domain.AppUser;

namespace Domain.Entities
{
    public class Member
    {
        public int MemberId { get; set; }
        public string Name { get; set; }

        public int RoomId { get; set; }
        public Room Room { get; set; }

        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

        public ICollection<Settlement>? Expenses { get; set; }
    }

}