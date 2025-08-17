using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomExpenseTracker.Data;
using RoomExpenseTracker.Models;
using RoomExpenseTracker.Models.AppUser;
using RoomExpenseTracker.ViewModels;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoomsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var rooms = await _context.Rooms
                .Where(r => r.Members.Any(m => m.ApplicationUserId == userId) && r.IsDeleted == false)
                .ToListAsync();


            foreach (var room in rooms)
            {
                room.Members = await _context.Members
                    .Where(m => m.RoomId == room.RoomId)
                    .ToListAsync();
            }
            return View(rooms);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            var room = new Room
            {
                CreatedByUser = user,
                Name = viewModel.Name,
                CreatedByUserId = userId
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            _context.Members.Add(new Member
            {
                Name = user.UserName,
                RoomId = room.RoomId,
                ApplicationUserId = userId
            });

            foreach (var username in viewModel.MemberUserNames.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                var existingUser = await _userManager.FindByNameAsync(username);

                _context.Members.Add(new Member
                {
                    Name = username,
                    RoomId = room.RoomId,
                    ApplicationUserId = existingUser?.Id
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id, string? month, bool isFromSettled = false)
        {
            var userId = _userManager.GetUserId(User);
            var room = await _context.Rooms
                .Include(r => r.Members)
                .Include(r => r.Expenses).ThenInclude(e => e.Member)
                .FirstOrDefaultAsync(r => r.RoomId == id && r.Members.Any(m => m.ApplicationUserId == userId));

            if (room == null)
                return RedirectToAction("AccessDenied", "Account");

            var months = room.Expenses.Select(e => e.Date.ToString("yyyy-MM")).Distinct().OrderByDescending(m => m).ToList();

            var vm = new RoomDetailsViewModel
            {
                Room = room,
                AvailableMonths = months,
                SelectedMonth = month ?? months.FirstOrDefault(),
                IsFromSettled = isFromSettled
            };

            return View(vm);
        }
    }

}