namespace Services.ViewModels
{
    public class RoomViewModel
    {
        [Required]
        public string Name { get; set; }

        public List<InviteMemberViewModel> Members { get; set; } = new();
    }

    public class InviteMemberViewModel
    {
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
