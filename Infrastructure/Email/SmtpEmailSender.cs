using Serilog;

namespace Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        public SmtpEmailSender(SmtpSettings? settings)
        {
            _settings = settings ?? new SmtpSettings();
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                Log.Warning("SMTP Host is not configured. Skipping email send to {Email}", email);
                return;
            }

            try
            {
                using (var client = new SmtpClient(_settings.Host, _settings.Port))
                {
                    client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
                    client.EnableSsl = _settings.EnableSSL;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_settings.FromEmail, _settings.FromName),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in SendEmailAsync for email {Email}: {Message}", email, ex.Message);
            }
        }
        public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string fileName)
        {
            try
            {
                using (var client = new SmtpClient(_settings.Host, _settings.Port))
                {
                    client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
                    client.EnableSsl = _settings.EnableSSL;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_settings.FromEmail, _settings.FromName),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);

                    if (attachment != null && attachment.Length > 0)
                    {
                        mailMessage.Attachments.Add(new Attachment(new MemoryStream(attachment), fileName));
                    }

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Exception in SendEmailWithAttachmentAsync: {ex.Message}");
            }
        }
    }
}
