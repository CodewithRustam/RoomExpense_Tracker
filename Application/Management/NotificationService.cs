namespace Services.Management
{
    public class NotificationService
    {
        public async Task SendExpenseNotificationAsync(List<string> deviceTokens, string? expenseName, decimal amount, string UserName, string? roomName, bool isUpdated = false)
        {
            var tasks = deviceTokens.Select(token => FirebaseMessaging.DefaultInstance.SendAsync(new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = isUpdated ? "Expense Updated" : "New Expense Added",
                    Body = $"{UserName} {(isUpdated ? "updated" : "added")} an expense of ₹{amount} for {expenseName} in {roomName}."
                }
            }));
            await Task.WhenAll(tasks);
        }
    }
}
