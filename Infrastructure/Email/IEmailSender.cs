namespace Infrastructure.Email
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string fileName);
    }
}
