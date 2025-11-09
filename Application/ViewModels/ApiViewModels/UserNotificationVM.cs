namespace Services.ViewModels.ApiViewModels
{
    public class UserNotificationVM
    {
        public int RoomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

    }
}
