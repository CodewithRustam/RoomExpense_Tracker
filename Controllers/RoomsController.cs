using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomExpenseTracker.Data;
using RoomExpenseTracker.Models;
using RoomExpenseTracker.Models.AppUser;
using RoomExpenseTracker.Services;
using RoomExpenseTracker.ViewModels;
using System.Globalization;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DailyReportService _dailyReportService;

        public RoomsController(AppDbContext context, UserManager<ApplicationUser> userManager, DailyReportService _dailyReportService)
        {
            _context = context;
            _userManager = userManager;
            this._dailyReportService = _dailyReportService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var rooms = await _context.Rooms.Include(r => r.Members).Where(r => r.Members.Any(m => m.ApplicationUserId == userId)).ToListAsync();

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
        public async Task<IActionResult> Details(int id, string? month)
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
                SelectedMonth = month ?? months.FirstOrDefault()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DisplayExpenses(int id, string month)
        {
            if (id <= 0)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var userId = _userManager.GetUserId(User);

            if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var selectedMonth))
                return BadRequest("Invalid month format.");

            var expenses = _context.Expenses.Where(x => x.RoomId == id && x.Date.Year == selectedMonth.Year && x.Date.Month == selectedMonth.Month)
                           .Select(x => new Expense
                           {
                               Date = x.Date,
                               Amount = x.Amount,
                               Member = x.Member,
                               Item = x.Item
                           }).ToList();

            List<ExpenseSummary> expensesSummary = new List<ExpenseSummary>();    
            if (expenses != null)
            {
                expensesSummary = expenses.GroupBy(x => x.Member.Name).Select(y => new ExpenseSummary
                {
                    MemberName = y.Key,
                    Total = y.Sum(x => x.Amount),
                    Items = y.OrderBy(x => x.Date).ToList()
                }).ToList();
            }

            if(expensesSummary == null || !expensesSummary.Any())
            {
                return PartialView("_DisplayRoomExpenses", new RoomExpensesViewModel());
            }

            decimal? total = expenses?.Sum(x => x.Amount);
            int memberCount = await _context.Members.CountAsync(rm => rm.RoomId == id);
            decimal? avgAmount = memberCount > 0 ? total / memberCount : 0m;

            RoomExpensesViewModel roomExpenseVM = new RoomExpensesViewModel
            {
                Summary = expensesSummary,
                TotalExpense = total,
                AvgPerPerson = avgAmount,
            };

            return PartialView("_DisplayRoomExpenses", roomExpenseVM);
        }
    }

}