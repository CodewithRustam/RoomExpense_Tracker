namespace Services.Management
{
    public class SettlementNotificationService : ISettlementNotificationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SettlementNotificationService> _logger;

        public SettlementNotificationService(IServiceScopeFactory scopeFactory, ILogger<SettlementNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
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

                    if (receiverUser != null && !string.IsNullOrWhiteSpace(receiverUser.Email))
                    {
                        string receiverSubject = $"Settlement Received - {settlementMonth:MMMM yyyy}";
                        string receiverBody = EmailTemplates.GetSettlementEmailTemplate(
                            receiverUser.UserName!,
                            $"{payerName} has settled ₹{amount:F2} with you for {settlementMonth:MMMM yyyy}.",
                            "Settlement Received", roomId);

                        await emailSender.SendEmailAsync(receiverUser.Email, receiverSubject, receiverBody);
                    }

                    if (payerUser != null && !string.IsNullOrWhiteSpace(payerUser.Email))
                    {
                        string payerSubject = $"Settlement Paid - {settlementMonth:MMMM yyyy}";
                        string payerBody = EmailTemplates.GetSettlementEmailTemplate(
                            payerUser.UserName!,
                            $"You have successfully settled ₹{amount:F2} to {receiverName} for {settlementMonth:MMMM yyyy}.",
                            "Settlement Paid", roomId);

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