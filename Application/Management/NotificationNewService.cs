namespace Services.Management
{
    public class NotificationNewService : INotificationServices
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly UserManager<ApplicationUser> _userManager;

        private IRepository<PushNotification> NotificationRepo => _uow.Repository<PushNotification>();

        public NotificationNewService(
            IUnitOfWork uow,
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager)
        {
            _uow = uow;
            _currentUser = currentUser;
            _userManager = userManager;
        }

        public async Task<ApiResponse> GetNotificationsAsync()
        {
            string? userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail("User not found.");

            var notifications = await NotificationRepo.GetAllAsync(n => n.UserId == userId);

            var sortedNotifications = notifications.OrderByDescending(n => n.SentAt).ToList();

            return ApiResponse<IEnumerable<PushNotification>>.SuccessRes(sortedNotifications, "Notifications fetched successfully.");
        }

        public async Task<ApiResponse> MarkAllReadAsync()
        {
            string? userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail("User not found.");

            var unreadNotifications = await NotificationRepo.GetAllAsync(n => n.UserId == userId && !n.IsRead);

            if (!unreadNotifications.Any())
                return ApiResponse.SuccessRes("All notifications are already read.");

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                NotificationRepo.Update(notification);
            }

            await _uow.SaveAsync();
            return ApiResponse.SuccessRes("Notifications marked as read.");
        }

        public async Task<ApiResponse> ClearAllAsync()
        {
            string? userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail("User not found.");

            var allNotifications = await NotificationRepo.GetAllAsync(n => n.UserId == userId);

            if (!allNotifications.Any())
                return ApiResponse.SuccessRes("No notifications to clear.");

            // Assuming your generic repository has a DeleteRange/RemoveRange method. 
            // If not, you can loop and call NotificationRepo.Delete() or add DeleteRange to the repo.
            foreach (var note in allNotifications)
            {
                NotificationRepo.Delete(note);
            }

            await _uow.SaveAsync();
            return ApiResponse.SuccessRes("All notifications cleared.");
        }

        public async Task<ApiResponse> DeleteNotificationAsync(int notificationId)
        {
            string? userId = _currentUser.UserId;

            var notification = await NotificationRepo.GetByIdAsync(notificationId);

            if (notification == null)
                return ApiResponse.Fail("Notification not found.");

            if (notification.UserId != userId)
                return ApiResponse.Fail("Unauthorized to delete this notification.");

            NotificationRepo.Delete(notification);
            await _uow.SaveAsync();

            return ApiResponse.SuccessRes("Notification deleted.");
        }

        public async Task<ApiResponse> RegisterDeviceAsync(string deviceToken)
        {
            string? userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail("User not found.");

            if (string.IsNullOrWhiteSpace(deviceToken)) return ApiResponse.Fail("Invalid device token.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ApiResponse.Fail("User not found in system.");

            user.DeviceToken = deviceToken;
            user.UpdatedBy = $"Updated by: {userId}";
            user.UpdatedDate = DateTimeProvider.NowIST;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return ApiResponse.Fail("Failed to register device token.");

            return ApiResponse.SuccessRes("Device registered successfully.");
        }
    }
}
