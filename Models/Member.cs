// Models/Member.cs
using RoomExpenseTracker.Models.AppUser;

namespace RoomExpenseTracker.Models
{
    public class Member
    {
        public int MemberId { get; set; }
        public string Name { get; set; }

        public int RoomId { get; set; }
        public Room Room { get; set; }

        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

        public ICollection<Expense>? Expenses { get; set; }
    }

}