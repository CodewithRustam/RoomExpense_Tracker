using Microsoft.AspNetCore.Routing.Tree;
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
            var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            while (!stoppingToken.IsCancellationRequested)
            {
                var utcNow = DateTime.UtcNow;
                var indiaNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, indiaTimeZone);

                var todayTargetTime = indiaNow.Date.AddHours(22).AddMinutes(30);
                DateTime nextRun;

                if (indiaNow < todayTargetTime)
                {
                    nextRun = todayTargetTime;
                }
                else
                {
                    nextRun = todayTargetTime.AddDays(1);
                }

                var delay = nextRun - indiaNow;

                if (delay.TotalMilliseconds > 0)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        context.DailyReportLogs.Add(new DailyReportLog
                        {
                            RunDate = indiaNow,
                            ReportName = "Monthly Expense Report",
                            Status = $"Waiting for {delay.TotalMinutes:F1} min",
                            Message = $"Next run scheduled at: {nextRun} IST"
                        });
                        await context.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    await Task.Delay(delay, stoppingToken);
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    context.DailyReportLogs.Add(new DailyReportLog
                    {
                        RunDate = indiaNow,
                        ReportName = "Monthly Expense Report",
                        Status = "Started",
                        Message = "Report generating and sending started."
                    });
                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception)
                {
                    throw;
                }

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

                try
                {
                    var csvContent = GenerateUserCsvReport(user, DateTime.Today);

                    if (!string.IsNullOrWhiteSpace(csvContent))
                    {
                        await SendEmailToUserAsync(user, csvContent, stoppingToken);

                        context.DailyReportLogs.Add(new DailyReportLog
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            RunDate = DateTime.UtcNow,
                            ReportName = "Monthly Expense Report",
                            Status = "Success",
                            Message = "Report sent successfully"
                        });
                    }
                    else
                    {
                        context.DailyReportLogs.Add(new DailyReportLog
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            RunDate = DateTime.UtcNow,
                            ReportName = "Monthly Expense Report",
                            Status = "Skipped",
                            Message = "No data to send"
                        });
                    }
                }
                catch (Exception ex)
                {
                    context.DailyReportLogs.Add(new DailyReportLog
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        RunDate = DateTime.UtcNow,
                        ReportName = "Monthly Expense Report",
                        Status = "Failed",
                        Message = ex.Message
                    });
                }

                await context.SaveChangesAsync(stoppingToken); 
            }
        }

        private string GenerateUserCsvReport(ApplicationUser user, DateTime referenceDate)
        {
            var sb = new StringBuilder();
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var expensesByRoom = context.Expenses
                                .Where(e => e.Member.ApplicationUserId == user.Id && (e.IsDeleted == false || e.IsDeleted == null) &&
                                            e.Date.Year == referenceDate.Year &&
                                            e.Date.Month == referenceDate.Month).Include(e => e.Room) 
                                .ToList().GroupBy(e => new { e.RoomId, e.Room.Name }).OrderBy(g => g.Key.RoomId).ToList();

            if (!expensesByRoom.Any()) return string.Empty;

            bool isFirstRoom = true;
            foreach (var roomGroup in expensesByRoom)
            {
                if (!isFirstRoom)
                {
                    sb.AppendLine(); 
                }
                sb.AppendLine($"Room {roomGroup.Key.Name} Expenses");
                sb.AppendLine("Item,Amount,ExpenseDate");

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
            message.To.Add(new MailboxAddress(user.UserName, user.Email+"@gmail.com"));

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
            catch (Exception)
            {
            }
            finally
            {
                await client.DisconnectAsync(true, stoppingToken);
            }
        }
    }
}
