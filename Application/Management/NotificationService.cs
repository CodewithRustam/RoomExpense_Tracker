public class NotificationService
{
    public async Task<(string Title, string Body)> SendExpenseNotificationAsync(
        List<string> deviceTokens,
        string? expenseName,
        decimal amount,
        string userName,
        string? roomName,
        int roomId,
        bool isUpdated = false)
    {
        var title = isUpdated ? "Expense Updated" : "New Expense Added";
        var body = $"{userName} {(isUpdated ? "updated" : "added")} an expense of ₹{amount} for {expenseName} in {roomName}.";

        if (deviceTokens != null && deviceTokens.Any())
        {
            var tasks = deviceTokens.Select(token => FirebaseMessaging.DefaultInstance.SendAsync(new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string> { { "roomId", roomId.ToString() } }
            }));

            await Task.WhenAll(tasks);
        }

        return (title, body);
    }
}