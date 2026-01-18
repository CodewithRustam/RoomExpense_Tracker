
using Serilog;

namespace Services.BackgroundJobs
{
    public interface IExpenseReportJob
    {
        Task ExecuteAsync(CancellationToken cancellationToken);
    }

    public class ExpenseReportJob : IExpenseReportJob
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public ExpenseReportJob(AppDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var runId = Guid.NewGuid().ToString().Substring(0, 8);
            var indiaNow = DateTimeProvider.NowIST;

            try
            {
                await LogStatusAsync(indiaNow, "Started", $"Report generation started [RunID: {runId}]");

                await GenerateAndSendReportsAsync(stoppingToken);

                await LogStatusAsync(DateTimeProvider.NowIST, "Completed", $"Report generation completed [RunID: {runId}]");
            }
            catch (Exception ex)
            {
                Log.Error($"Error: { ex.Message} [RunID: { runId}]");
                await LogStatusAsync(DateTimeProvider.NowIST, "Failed", $"Error: {ex.Message} [RunID: {runId}]");
            }
        }

        private async Task LogStatusAsync(DateTime runDate, string status, string message)
        {
            try
            {
                _context.DailyReportLogs.Add(new DailyReportLog
                {
                    RunDate = runDate,
                    ReportName = "Monthly Expense Report",
                    Status = status,
                    Message = message,
                    CreatedAt = DateTimeProvider.NowIST
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"Error in LogStatusAsync: {ex.Message}");
            }
        }

        private async Task GenerateAndSendReportsAsync(CancellationToken stoppingToken)
        {
            var users = await _context.Users.ToListAsync(stoppingToken);

            foreach (var user in users)
            {
                stoppingToken.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(user.Email)) continue;

                try
                {
                    var pdfContent = GenerateUserPdfReport(user, DateTime.Today);

                    if (pdfContent != null && pdfContent.Length > 0)
                    {
                        await SendEmailToUserAsync(user, pdfContent, stoppingToken);
                        await LogStatusAsync(DateTimeProvider.NowIST, "Success", $"Report sent to {user.Email}");
                    }
                    else
                    {
                        await LogStatusAsync(DateTimeProvider.NowIST, "Skipped", $"No data for {user.Email}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Exception in GenerateAndSendReportsAsync: {ex.Message}, Failed for {user.Email}");
                    await LogStatusAsync(DateTimeProvider.NowIST, "Failed User", $"Failed for {user.Email}: {ex.Message}");
                }
            }
        }

        private byte[] GenerateUserPdfReport(ApplicationUser user, DateTime referenceDate)
        {
            var expensesByRoom = _context.Expenses
                .Where(e => e.Member.ApplicationUserId == user.Id &&
                            (e.IsDeleted == false || e.IsDeleted == null) &&
                            e.Date.Year == referenceDate.Year &&
                            e.Date.Month == referenceDate.Month)
                .Include(e => e.Room)
                .ToList()
                .GroupBy(e => new { e.RoomId, RoomName = e.Room.Name })
                .OrderBy(g => g.Key.RoomName)
                .ToList();

            if (!expensesByRoom.Any()) return Array.Empty<byte>();

            decimal grandTotal = expensesByRoom.Sum(group => group.Sum(e => e.Amount));

            var primaryColor = Colors.Blue.Medium;
            var accentColor = Colors.Blue.Lighten4;
            var headerTextColor = Colors.White;
            var lightGrey = Colors.Grey.Lighten3;
            var darkGrey = Colors.Grey.Darken2;

            TextStyle HeaderStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(primaryColor);
            TextStyle SubHeaderStyle = TextStyle.Default.FontSize(14).SemiBold().FontColor(darkGrey);
            TextStyle TableHeaderStyle = TextStyle.Default.SemiBold().FontColor(darkGrey);
            TextStyle TotalLabelStyle = TextStyle.Default.SemiBold().FontColor(darkGrey);
            TextStyle TotalValueStyle = TextStyle.Default.FontSize(12).Bold().FontColor(primaryColor);

            using var memoryStream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().PaddingBottom(20).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(titleCol =>
                            {
                                titleCol.Item().Text($"Monthly Expense Report").Style(HeaderStyle);

                                titleCol.Item().Text($"{referenceDate:MMMM yyyy}").Style(TextStyle.Default.FontSize(14).FontColor(Colors.Grey.Medium));
                            });

                            row.RelativeItem().AlignRight().Column(userCol =>
                            {
                                userCol.Item().Text(text =>
                                {
                                    text.Span("User: ").SemiBold();
                                    text.Span($"{user.UserName}");
                                });
                                var userEmail = user.Email?.ToLower() ?? "N/A";
                                userCol.Item().Text(text =>
                                {
                                    text.Span("Email: ").SemiBold();
                                    text.Span(userEmail);
                                });
                            });
                        });
                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor(primaryColor);
                    });

                    page.Content().PaddingVertical(10).Column(mainCol =>
                    {
                        mainCol.Spacing(20);

                        foreach (var roomGroup in expensesByRoom)
                        {
                            decimal roomTotal = roomGroup.Sum(e => e.Amount);

                            mainCol.Item().Column(roomSection =>
                            {
                                roomSection.Item().PaddingBottom(5).Row(row =>
                                {
                                    row.RelativeItem().Text(roomGroup.Key.RoomName?.ToUpper()).Style(SubHeaderStyle);
                                });

                                roomSection.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3); // Item gets more space
                                        columns.ConstantColumn(80); // Date
                                        columns.ConstantColumn(100); // Amount
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(TableHeaderCell).Text("Description");
                                        header.Cell().Element(TableHeaderCell).AlignCenter().Text("Date");
                                        header.Cell().Element(TableHeaderCell).AlignRight().Text("Amount");
                                    });

                                    var expensesList = roomGroup.OrderBy(e => e.Date).ToList();
                                    for (int i = 0; i < expensesList.Count; i++)
                                    {
                                        var expense = expensesList[i];
                                        var bgColor = i % 2 == 1 ? lightGrey : Colors.White;

                                        table.Cell().Element(c => TableDataCell(c, bgColor)).Text(expense.Item);
                                        table.Cell().Element(c => TableDataCell(c, bgColor)).AlignCenter().Text(expense.Date.ToString("MMM dd, yyyy"));
                                        table.Cell().Element(c => TableDataCell(c, bgColor)).AlignRight().Text(expense.Amount.ToString("N2"));
                                    }
                                });

                                roomSection.Item().PaddingTop(5).Row(row =>
                                {
                                    row.RelativeItem();
                                    row.ConstantItem(150).BorderTop(1).BorderColor(Colors.Grey.Medium).PaddingTop(5).Row(totalRow =>
                                    {
                                        totalRow.RelativeItem().Text("Subtotal:").Style(TotalLabelStyle).AlignRight();
                                        totalRow.ConstantItem(10);
                                        totalRow.RelativeItem().Text(roomTotal.ToString("N2")).Style(TotalValueStyle).AlignRight();
                                    });
                                });
                            });
                        }

                        mainCol.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                        mainCol.Item().Background(accentColor).Padding(15).Row(row =>
                        {
                            row.RelativeItem().Column(col => {
                                col.Item().Text("Report Status: Completed").Style(TextStyle.Default.FontSize(9).FontColor(darkGrey));
                                col.Item().Text($"Generated on {DateTime.Now:g}").Style(TextStyle.Default.FontSize(9).FontColor(darkGrey));
                            });

                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("GRAND TOTAL").Style(TotalLabelStyle).FontSize(14);
                                col.Item().Text(grandTotal.ToString("C2")).Style(TotalValueStyle).FontSize(20);
                            });
                        });
                    });

                    page.Footer().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Expense Management System").Style(TextStyle.Default.FontSize(9).FontColor(Colors.Grey.Medium));
                        });

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                    });
                });
            })
            .GeneratePdf(memoryStream);

            return memoryStream.ToArray();

            IContainer TableHeaderCell(IContainer container)
            {
                return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Medium);
            }

            IContainer TableDataCell(IContainer container, string backgroundColor)
            {
                return container
                    .Background(backgroundColor)
                    .PaddingVertical(5)
                    // Optional: lighter vertical borders between columns
                    //.BorderRight(1).BorderColor(Colors.Grey.Lighten4)
                    .PaddingHorizontal(2);
            }
        }
        private async Task SendEmailToUserAsync(ApplicationUser user, byte[] pdfContent, CancellationToken stoppingToken)
        {
            var subject = $"Your Monthly Expense Report - {DateTime.Today:MMMM yyyy}";
            var body = "Please find your monthly expense report attached.";

            if (user is not null && !string.IsNullOrEmpty(user.Email))
            {
                await _emailSender.SendEmailWithAttachmentAsync(user.Email, subject, body, pdfContent,
                       $"MonthlyReport_{user.UserName}_{DateTime.Today:yyyy-MM}.pdf");
            }
        }
    }
}