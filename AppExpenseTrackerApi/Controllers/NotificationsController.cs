using AppExpenseTracker.Controllers;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AppExpenseTrackerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] UserNotificationVM userNotificationVM)
        {
            //var usersInRoom = await _context.Members.Where(r => r.RoomId == userNotificationVM.RoomId).Select(r => r.ApplicationUserId).ToListAsync();

            //foreach (var userId in usersInRoom)
            //{
            //    _context.Notifications.Add(new PushNotification
            //    {
            //        UserId = userId ?? string.Empty,
            //        Title = userNotificationVM.Title,
            //        Body = userNotificationVM.Body,
            //        SentAt = DateTimeProvider.NowIST,
            //        IsRead = false
            //    });
            //}

            //await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessRes("Notification added"));
        }

        [HttpGet("get-notifications")]
        public async Task<IActionResult> GetNotifications(string userId)
        {
            var list = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentAt)
                .ToListAsync();

            return Ok(list);
        }

        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllRead(string userId)
        {
            var notes = _context.Notifications.Where(n => n.UserId == userId);
            foreach (var n in notes) n.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("clear-all")]
        public async Task<IActionResult> ClearAll(string userId)
        {
            var notes = _context.Notifications.Where(n => n.UserId == userId);
            _context.Notifications.RemoveRange(notes);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpDelete("delete-notification")]
        public async Task<IActionResult> DeleteNotification(int notificationId)
        {
            var notification = _context.Notifications.Where(n => n.Id == notificationId).FirstOrDefault();
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
        [HttpPost("app-register")]
        public async Task<IActionResult> RegisterDevice([FromBody] DeviceTokenModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId!);
            if (user == null) return BadRequest();

            user.DeviceToken = model.DeviceToken;
            user.UpdatedBy = $"Updated by: {model.UserId}";
            user.UpdatedDate = DateTimeProvider.NowIST;
            await _userManager.UpdateAsync(user);

            return Ok();
        }
    }
}
