namespace AppExpenseTrackerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationServices _notificationServices;

        public NotificationsController(INotificationServices notificationServices)
        {
            _notificationServices = notificationServices;
        }

        [HttpGet("get-notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var response = await _notificationServices.GetNotificationsAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var response = await _notificationServices.MarkAllReadAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAll()
        {
            var response = await _notificationServices.ClearAllAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpDelete("delete-notification/{notificationId}")]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var response = await _notificationServices.DeleteNotificationAsync(notificationId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("app-register")]
        public async Task<IActionResult> RegisterDevice([FromBody] DeviceTokenModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.DeviceToken))
                return BadRequest(ApiResponse.Fail("Device token is required."));

            var response = await _notificationServices.RegisterDeviceAsync(model.DeviceToken);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}