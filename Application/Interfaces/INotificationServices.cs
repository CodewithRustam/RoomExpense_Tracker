namespace Services.Interfaces
{
    public interface INotificationServices
    {
        Task<ApiResponse> GetNotificationsAsync();
        Task<ApiResponse> MarkAllReadAsync();
        Task<ApiResponse> ClearAllAsync();
        Task<ApiResponse> DeleteNotificationAsync(int notificationId);
        Task<ApiResponse> RegisterDeviceAsync(string deviceToken);
    }
}