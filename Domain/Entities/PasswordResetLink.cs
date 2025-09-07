namespace Domain.Entities
{
    public class PasswordResetLink
    {
        public int Id { get; set; }
        public string ShortCode { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
    }
}
