using Infrastructure;

namespace Services.Management
{
    public class ExpenseNotificationService : IExpenseNotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpenseNotificationService> _logger;

        public ExpenseNotificationService(IServiceScopeFactory scopeFactory, ILogger<ExpenseNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void FireAndForgetExpenseNotification(Expense expense, string userId, string userName, bool isUpdate = false)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                    if (!Convert.ToBoolean(Environment.GetEnvironmentVariable("EnableNotifications") ?? "false")) return;

                    var notifService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                    var memberRepo = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
                    var roomRepo = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var tokens = await memberRepo.GetDeviceTokensAsync(expense.RoomId, userId);
                    if (tokens == null || !tokens.Any()) return;

                    var roomName = await roomRepo.GetRoomNameAsync(expense.RoomId);

                    var result = await notifService.SendExpenseNotificationAsync(
                        tokens.Select(t => t!).ToList(), expense.Item, expense.Amount, userName, roomName, expense.RoomId, isUpdate);

                    var members = await memberRepo.GetAllAsync(m => m.RoomId == expense.RoomId && m.ApplicationUserId != userId);
                    if (members == null || !members.Any()) return;

                    var notifications = members.Select(m => new PushNotification
                    {
                        UserId = m.ApplicationUserId!,
                        Title = result.Title,
                        Body = result.Body,
                        SentAt = DateTimeProvider.NowIST,
                        IsRead = false
                    }).ToList();

                    await uow.Repository<PushNotification>().AddRangeAsync(notifications);
                    await uow.SaveAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background notification failed for expense {Id}", expense.ExpenseId);
                }
            });
        }
    }
}