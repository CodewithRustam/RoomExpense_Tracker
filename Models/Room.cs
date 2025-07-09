using RoomExpenseTracker.Models.AppUser;
using System.ComponentModel.DataAnnotations;

namespace RoomExpenseTracker.Models
{
    public class Room
    {
        public int RoomId { get; set; }
        [Required]
        public string? Name { get; set; }
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }
        public List<Member> Members { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();
    }
}