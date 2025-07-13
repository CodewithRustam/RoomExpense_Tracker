using Microsoft.EntityFrameworkCore;
using MimeKit;
using RoomExpenseTracker.Data;
using RoomExpenseTracker.Models;
using System.Text;

namespace RoomExpenseTracker.Services
{
    public class DailyReportService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public DailyReportService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddHours(0); 
                var delay = nextRun - now;

                if (delay.TotalMilliseconds > 0)
                    await Task.Delay(delay, stoppingToken);

                await GenerateAndSendReportsAsync(stoppingToken);
            }
        }

        private async Task GenerateAndSendReportsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rooms = await context.Rooms
                .Include(r => r.Members)
                .ThenInclude(m => m.ApplicationUser)
                .Include(r => r.Expenses)
                .ThenInclude(e => e.Member)
                .ToListAsync(stoppingToken);

            foreach (var room in rooms)
            {
                if (stoppingToken.IsCancellationRequested) break;

                var csvContent = GenerateCsvReport(room, DateTime.Today.AddDays(-1));
                await SendEmailAsync(room, csvContent, stoppingToken);
            }
        }

        private string GenerateCsvReport(Room room, DateTime date)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MemberName,ExpenseDate,Amount,Description");

            var expenses = room.Expenses
                .Where(e => e.Date.Date == date.Date)
                .OrderBy(e => e.Member.Name)
                .ThenBy(e => e.Date);

            foreach (var expense in expenses)
            {
                sb.AppendLine($"\"{expense.Member.Name}\",\"{expense.Date:yyyy-MM-dd}\",\"{expense.Amount:C2}\"");
            }

            return sb.ToString();
        }

        private async Task SendEmailAsync(Room room, string csvContent, CancellationToken stoppingToken)
        {
            var emailConfig = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailConfig["SenderName"], emailConfig["SenderEmail"]));

            var creator = room.Members.FirstOrDefault(m => m.ApplicationUserId == room.CreatedByUserId)?.ApplicationUser;
            if (creator?.Email != null)
            {
                message.To.Add(new MailboxAddress(creator.UserName, "rustamali637@gmail.com"));
            }
            else
            {
                return;
            }

            message.Subject = $"Daily Expense Report for {room.Name} - {DateTime.Today.AddDays(-1):yyyy-MM-dd}";
            var body = new TextPart("plain") { Text = "Please find attached the daily expense report." };

            var attachment = new MimePart("text", "csv")
            {
                Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes(csvContent))),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                FileName = $"DailyReport_{room.Name}_{DateTime.Today.AddDays(-1):yyyy-MM-dd}.csv"
            };

            var multipart = new Multipart("mixed") { body, attachment };
            message.Body = multipart;

            using var client = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                if (!int.TryParse(emailConfig["SmtpPort"], out int smtpPort))
                {
                    smtpPort = 587;
                }

                await client.ConnectAsync(emailConfig["SmtpServer"], smtpPort, MailKit.Security.SecureSocketOptions.StartTls, stoppingToken);
                await client.AuthenticateAsync(emailConfig["SmtpUsername"], emailConfig["SmtpPassword"], stoppingToken);
                await client.SendAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                return;
            }
            finally
            {
                await client.DisconnectAsync(true, stoppingToken);
            }
        }
    }
}
