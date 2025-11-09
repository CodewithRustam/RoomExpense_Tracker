using ExpenseTrakcerHepler;

namespace Domain.Entities
{
    public class PushNotification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTimeProvider.NowIST;
        public bool IsRead { get; set; } = false;
    }
}
