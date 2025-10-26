using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Domain.AppUser
{
    public class ApplicationUser: IdentityUser
    {
        public ICollection<Member>? MemberRooms { get; set; }
        public string? DeviceToken { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
