using Microsoft.AspNetCore.Identity;

namespace RoomExpenseTracker.Models.AppUser
{
    public class ApplicationUser: IdentityUser
    {
        public ICollection<Member>? MemberRooms { get; set; }
    }
}
