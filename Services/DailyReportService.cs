using Microsoft.EntityFrameworkCore;
using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using RoomExpenseTracker.Data;
using RoomExpenseTracker.Models;
using RoomExpenseTracker.Models.AppUser;
using LicenseType = QuestPDF.Infrastructure.LicenseType;

namespace RoomExpenseTracker.Services
{
    public class DailyReportService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private Timer _timer;

        public DailyReportService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            // Required for QuestPDF
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ScheduleNextRun();
            //RunJobAsync();
            return Task.CompletedTask;
        }

        private void ScheduleNextRun()
        {
            var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var utcNow = DateTime.UtcNow;
            var indiaNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, indiaTimeZone);

            var todayTargetTime = indiaNow.Date.AddHours(22).AddMinutes(50);
            DateTime nextRun = indiaNow < todayTargetTime ? todayTargetTime : todayTargetTime.AddDays(1);

            var delay = nextRun - indiaNow;

            _timer = new Timer(async _ =>
            {
                await RunJobAsync();
                ScheduleNextRun();
            }, null, delay, Timeout.InfiniteTimeSpan);
        }

        private async Task RunJobAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                context.DailyReportLogs.Add(new DailyReportLog
                {
                    RunDate = DateTime.UtcNow,
                    ReportName = "Monthly Expense Report",
                    Status = "Started",
                    Message = "Report generation started"
                });
                await context.SaveChangesAsync();

                await GenerateAndSendReportsAsync(CancellationToken.None);

                context.DailyReportLogs.Add(new DailyReportLog
                {
                    RunDate = DateTime.UtcNow,
                    ReportName = "Monthly Expense Report",
                    Status = "Completed",
                    Message = "Report generation completed"
                });
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                context.DailyReportLogs.Add(new DailyReportLog
                {
                    RunDate = DateTime.UtcNow,
                    ReportName = "Monthly Expense Report",
                    Status = "Failed",
                    Message = ex.Message
                });
                await context.SaveChangesAsync();
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
                    var pdfContent = GenerateUserPdfReport(user, DateTime.Today);

                    if (pdfContent != null && pdfContent.Length > 0)
                    {
                        await SendEmailToUserAsync(user, pdfContent, stoppingToken);

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

        private byte[] GenerateUserPdfReport(ApplicationUser user, DateTime referenceDate)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var expensesByRoom = context.Expenses
                .Where(e => e.Member.ApplicationUserId == user.Id &&
                            (e.IsDeleted == false || e.IsDeleted == null) &&
                            e.Date.Year == referenceDate.Year &&
                            e.Date.Month == referenceDate.Month)
                .Include(e => e.Room)
                .ToList()
                .GroupBy(e => new { e.RoomId, e.Room.Name })
                .OrderBy(g => g.Key.RoomId)
                .ToList();

            if (!expensesByRoom.Any()) return Array.Empty<byte>();

            using var memoryStream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header()
                        .Text($"Monthly Expense Report - {referenceDate:MMMM yyyy}")
                        .FontSize(18).Bold().AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"User: {user.UserName}");
                        col.Item().Text($"Email: {user.Email}");

                        foreach (var roomGroup in expensesByRoom)
                        {
                            col.Item().Text($"Room: {roomGroup.Key.Name}").FontSize(14).Bold();

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(100);
                                    columns.ConstantColumn(120);
                                });

                                // header row
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Item").Bold();
                                    header.Cell().Element(CellStyle).Text("Amount").Bold();
                                    header.Cell().Element(CellStyle).Text("Expense Date").Bold();
                                });

                                // data rows
                                foreach (var expense in roomGroup.OrderBy(e => e.Date))
                                {
                                    table.Cell().Element(CellStyle).Text(expense.Item);
                                    table.Cell().Element(CellStyle).Text(expense.Amount.ToString("F2"));
                                    table.Cell().Element(CellStyle).Text(expense.Date.ToString("yyyy-MM-dd"));
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:dd MMM yyyy HH:mm}");
                });
            })
            .GeneratePdf(memoryStream);

            return memoryStream.ToArray();

            // Local cell style method
            QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
            {
                return container.Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2);
            }
        }

        private async Task SendEmailToUserAsync(ApplicationUser user, byte[] pdfContent, CancellationToken stoppingToken)
        {
            var emailConfig = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailConfig["SenderName"], emailConfig["SenderEmail"]));
            message.To.Add(new MailboxAddress(user.UserName, user.Email+"@gmail.com"));

            message.Subject = $"Your Monthly Expense Report - {DateTime.Today:MMMM yyyy}";
            var body = new TextPart("plain") { Text = "Please find your monthly expense report attached." };

            var attachment = new MimePart("application", "pdf")
            {
                Content = new MimeContent(new MemoryStream(pdfContent)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                FileName = $"MonthlyReport_{user.UserName}_{DateTime.Today:yyyy-MM}.pdf"
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
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                await client.DisconnectAsync(true, stoppingToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
