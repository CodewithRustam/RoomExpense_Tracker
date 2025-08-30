using System.ComponentModel.DataAnnotations;

namespace AppExpenseTracker.ViewModels
{
    public class RoomViewModel
    {
        [Required]
        public string? Name { get; set; }

        public List<string> MemberUserNames { get; set; } = new(); 
    }
}
