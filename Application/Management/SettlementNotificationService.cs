namespace Services.Management
{
    public class SettlementNotificationService : ISettlementNotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILogger<SettlementNotificationService> _logger;

        public SettlementNotificationService(
            IServiceScopeFactory scopeFactory,
            IEmailTemplateService emailTemplateService,
            ILogger<SettlementNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _emailTemplateService = emailTemplateService;
            _logger = logger;
        }

        public void FireAndForgetSettlementEmail(int roomId, string payerUserId, string payerName, string receiverUserId, string receiverName, decimal amount, DateTime settlementMonth)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    var payerUser = await userManager.FindByIdAsync(payerUserId);
                    var receiverUser = await userManager.FindByIdAsync(receiverUserId);
                    string monthFormatted = settlementMonth.ToString("MMMM yyyy");

                    if (receiverUser != null && !string.IsNullOrWhiteSpace(receiverUser.Email))
                    {
                        var (receiverSubject, receiverBody) = await _emailTemplateService.GetSettlementEmailAsync(
                            receiverUser.UserName ?? "User",
                            $"{payerName} has settled ₹{amount:F2} with you for {monthFormatted}.",
                            "Settlement Received", 
                            roomId, 
                            monthFormatted);

                        await emailSender.SendEmailAsync(receiverUser.Email, receiverSubject, receiverBody);
                    }

                    if (payerUser != null && !string.IsNullOrWhiteSpace(payerUser.Email))
                    {
                        var (payerSubject, payerBody) = await _emailTemplateService.GetSettlementEmailAsync(
                            payerUser.UserName ?? "User",
                            $"You have successfully settled ₹{amount:F2} to {receiverName} for {monthFormatted}.",
                            "Settlement Paid", 
                            roomId, 
                            monthFormatted);

                        await emailSender.SendEmailAsync(payerUser.Email, payerSubject, payerBody);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background settlement email failed for RoomId: {RoomId}", roomId);
                }
            });
        }
    }
}