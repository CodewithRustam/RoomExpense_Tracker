using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace Services.Management
{
    public class NotificationService
    {
        public async Task SendExpenseNotificationAsync(List<string> deviceTokens, string? expenseName, decimal amount, string UserName, string? roomName)
        {
            try
            {
                var tasks = deviceTokens.Select(token => FirebaseMessaging.DefaultInstance.SendAsync(new Message
                {
                    Token = token,
                    Notification = new Notification
                    {
                        Title = "New Expense Added",
                        Body = $"{UserName} added an expense of ₹{amount} for {expenseName} in {roomName}."
                    }
                }));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM ERROR] {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                throw;
            }
        }
    }
}
