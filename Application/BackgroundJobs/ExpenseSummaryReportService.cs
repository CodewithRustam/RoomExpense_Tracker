using Domain.AppUser;
using Domain.Entities;
using ExpenseTrakcerHepler;
using Infrastructure.Data;
using Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Services.BackgroundJobs
{
    public class ExpenseSummaryReportService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly IDateTimeProvider _dateTimeProvider;
        private Timer? _timer;

        public ExpenseSummaryReportService(IServiceProvider serviceProvider, IConfiguration configuration, IDateTimeProvider dateTimeProvider)
        {
            _serviceProvider = serviceProvider;
            QuestPDF.Settings.License = LicenseType.Community;
            _configuration = configuration;
            _dateTimeProvider = dateTimeProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ScheduleNextRun();
            //RunJobAsync();
            return Task.CompletedTask;
        }

        private async void ScheduleNextRun()
        {
            var indiaNow = _dateTimeProvider.NowIST;

            var targetHour = 21;
            var targetMinute = 30;

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            DateTime lastRunDate = context.DailyReportLogs
                .Where(x => x.Status == "Completed" && x.Message == "Report generation completed")
                .OrderByDescending(x => x.DailyReportLogId)
                .Select(x => x.CreatedAt)
                .FirstOrDefault();

            var next3DayRun = lastRunDate.AddDays(5).Date.AddHours(targetHour).AddMinutes(targetMinute);
            if (indiaNow > next3DayRun)
                next3DayRun = indiaNow.Date.AddHours(targetHour).AddMinutes(targetMinute).AddDays(3);

            var lastDayOfThisMonth = new DateTime(indiaNow.Year, indiaNow.Month,
                DateTime.DaysInMonth(indiaNow.Year, indiaNow.Month))
                .AddHours(targetHour).AddMinutes(targetMinute);

            var lastDayOfNextMonth = new DateTime(indiaNow.Year, indiaNow.Month, 1)
                .AddMonths(1)
                .AddDays(-1)
                .AddHours(targetHour).AddMinutes(targetMinute);

            var nextMonthEndRun = indiaNow <= lastDayOfThisMonth ? lastDayOfThisMonth : lastDayOfNextMonth;

            var nextRun = next3DayRun < nextMonthEndRun ? next3DayRun : nextMonthEndRun;

            var delay = nextRun - indiaNow;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero; 

            context.DailyReportLogs.Add(new DailyReportLog
            {
                RunDate = indiaNow,
                ReportName = "Monthly Expense Report",
                Status = "Started",
                Message = $"Scheduled for : {nextRun} (in {delay})",
                CreatedAt = indiaNow
            });
            await context.SaveChangesAsync();

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
                    RunDate = _dateTimeProvider.NowIST,
                    ReportName = "Monthly Expense Report",
                    Status = "Started",
                    Message = "Report generation started",
                    CreatedAt = _dateTimeProvider.NowIST
                });
                await context.SaveChangesAsync();

                await GenerateAndSendReportsAsync(CancellationToken.None);

                context.DailyReportLogs.Add(new DailyReportLog
                {
                    RunDate = _dateTimeProvider.NowIST,
                    ReportName = "Monthly Expense Report",
                    Status = "Completed",
                    Message = "Report generation completed",
                    CreatedAt = _dateTimeProvider.NowIST
                });
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                context.DailyReportLogs.Add(new DailyReportLog
                {
                    RunDate = _dateTimeProvider.NowIST,
                    ReportName = "Monthly Expense Report",
                    Status = "Failed",
                    Message = ex.Message,
                    CreatedAt = _dateTimeProvider.NowIST
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
                            RunDate = _dateTimeProvider.NowIST,
                            ReportName = "Monthly Expense Report",
                            Status = "Success",
                            Message = "Report sent successfully",
                            CreatedAt = _dateTimeProvider.NowIST
                        });
                    }
                    else
                    {
                        context.DailyReportLogs.Add(new DailyReportLog
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            RunDate = _dateTimeProvider.NowIST,
                            ReportName = "Monthly Expense Report",
                            Status = "Skipped",
                            Message = "No data to send",
                            CreatedAt = _dateTimeProvider.NowIST
                        });
                    }
                }
                catch (Exception ex)
                {
                    context.DailyReportLogs.Add(new DailyReportLog
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        RunDate = _dateTimeProvider.NowIST,
                        ReportName = "Monthly Expense Report",
                        Status = "Failed",
                        Message = ex.Message,
                        CreatedAt = _dateTimeProvider.NowIST
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
                        col.Item().Text($"Email: {user.Email!.ToLower()}@gmail.com");

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

            QuestPDF.Infrastructure.IContainer CellStyle(QuestPDF.Infrastructure.IContainer container)
            {
                return container.Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2);
            }
        }

        private async Task SendEmailToUserAsync(ApplicationUser user, byte[] pdfContent, CancellationToken stoppingToken)
        {
            var subject = $"Your Monthly Expense Report - {DateTime.Today:MMMM yyyy}";
            var body = "Please find your monthly expense report attached.";

           if(user is not null && !string.IsNullOrEmpty(user.Email))
           {
                using var scope = _serviceProvider.CreateScope();
                var _emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                await _emailSender.SendEmailWithAttachmentAsync(user.Email,subject,body,pdfContent,
                       $"MonthlyReport_{user.UserName}_{DateTime.Today:yyyy-MM}.pdf");
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
