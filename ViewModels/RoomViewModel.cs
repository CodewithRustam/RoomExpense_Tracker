using System.ComponentModel.DataAnnotations;

namespace RoomExpenseTracker.ViewModels
{
    public class RoomViewModel
    {
        [Required]
        public string? Name { get; set; }

        public List<string> MemberUserNames { get; set; } = new(); 
    }
}
