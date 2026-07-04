namespace Domain.Constants
{
    public static class ExpenseMessages
    {
        public const string MemberNotFound = "Member not found.";
        public const string RateLimitExceeded = "Too many expense submissions. Please wait.";
        public const string ExpenseExists = "This expense already exists.";
        public const string SaveFailed = "Failed to record expense.";
        public const string UpdateFailed = "Expense not updated.";
        public const string DeleteFailed = "Deletion failed.";
        public const string DailyDeleteLimit = "Daily delete limit reached.";
        public const string ExpenseNotFound = "Expense not found or permission denied.";
        public const string MonthSettled = "Cannot modify expense for a settled month.";
        public const string InvalidMonthFormat = "Invalid month format. Please use YYYY-MM format (e.g., 2025-10).";
        public const string InvalidRoomId = "Invalid room ID.";
        public const string NoMembersFound = "No members found.";
        public const string NoSettlementData = "No settlement data found.";
        public const string NoTrendData = "No expense trend data found.";
        public const string SuccessFetch = "Data fetched successfully.";
        public const string SuccessAdd = "Expense recorded successfully.";
        public const string SuccessUpdate = "Expense updated successfully.";
        public const string SuccessDelete = "Expense deleted successfully.";
    }
}
