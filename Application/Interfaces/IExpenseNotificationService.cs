namespace Services.Interfaces
{
    public interface IExpenseNotificationService
    {
        void FireAndForgetExpenseNotification(Expense expense, string userId, string userName, bool isUpdate = false);
    }
}