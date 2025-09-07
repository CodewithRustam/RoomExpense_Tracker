using Azure.Core;
using Infrastructure.Email.Config;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        public SmtpEmailSender(SmtpSettings settings)
        {
            _settings = settings;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
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
                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception)
            {
                throw;
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
            catch (Exception)
            {
                throw;
            }
        }
    }
}
