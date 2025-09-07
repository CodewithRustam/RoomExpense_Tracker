using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Domain.AppUser
{
    public class ApplicationUser: IdentityUser
    {
        public ICollection<Member>? MemberRooms { get; set; }
    }
}
