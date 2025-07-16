using Microsoft.EntityFrameworkCore;
using MimeKit;
using RoomExpenseTracker.Data;
using RoomExpenseTracker.Models;
using RoomExpenseTracker.Models.AppUser;
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
                var nextRun = now.Date.AddMinutes(2);
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
            var users = await context.Users.ToListAsync(stoppingToken);

            foreach (var user in users)
            {
                if (stoppingToken.IsCancellationRequested) break;

                if (string.IsNullOrEmpty(user.Email)) continue;

                var csvContent = GenerateUserCsvReport(user, DateTime.Today);

                if (!string.IsNullOrWhiteSpace(csvContent))
                {
                    await SendEmailToUserAsync(user, csvContent, stoppingToken);
                }
            }
        }

        private string GenerateUserCsvReport(ApplicationUser user, DateTime referenceDate)
        {
            var sb = new StringBuilder();
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Fetch data to memory before grouping
            var expensesByRoom = context.Expenses
                .Where(e => e.Member.ApplicationUserId == user.Id &&
                            e.Date.Year == referenceDate.Year &&
                            e.Date.Month == referenceDate.Month)
                .ToList() // Materialize the query to avoid translation issues
                .GroupBy(e => e.RoomId)
                .OrderBy(g => g.Key)
                .ToList();

            if (!expensesByRoom.Any()) return string.Empty;

            bool isFirstRoom = true;
            foreach (var roomGroup in expensesByRoom)
            {
                // Add a separator and room header between sheets
                if (!isFirstRoom)
                {
                    sb.AppendLine(); // Empty line for separation
                }
                sb.AppendLine($"Room {roomGroup.Key} Expenses");
                sb.AppendLine("Item,Amount,ExpenseDate");

                // Sort expenses within the room by date
                var sortedExpenses = roomGroup.OrderBy(e => e.Date);
                foreach (var expense in sortedExpenses)
                {
                    sb.AppendLine($"\"{expense.Item}\",\"{expense.Amount}\",\"{expense.Date:yyyy-MM-dd}\"");
                }
                isFirstRoom = false;
            }

            return sb.ToString();
        }
        private async Task SendEmailToUserAsync(ApplicationUser user, string csvContent, CancellationToken stoppingToken)
        {
            var emailConfig = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailConfig["SenderName"], emailConfig["SenderEmail"]));
            message.To.Add(new MailboxAddress(user.UserName, user.Email));

            message.Subject = $"Your Monthly Expense Report - {DateTime.Today:MMMM yyyy}";
            var body = new TextPart("plain") { Text = "Please find your monthly expense report attached." };

            var attachment = new MimePart("text", "csv")
            {
                Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes(csvContent))),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                FileName = $"MonthlyReport_{user.UserName}_{DateTime.Today:yyyy-MM}.csv"
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
                // Optionally log the error
            }
            finally
            {
                await client.DisconnectAsync(true, stoppingToken);
            }
        }
    }
}
