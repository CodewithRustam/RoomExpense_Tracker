using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomExpenseTracker.Data;
using RoomExpenseTracker.Models;
using RoomExpenseTracker.Models.AppUser;
using RoomExpenseTracker.ViewModels;
using System.Globalization;

namespace RoomExpenseTracker.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExpensesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int> GetMemberId(string? userId)
        {
            return await _context.Members.Where(x => x.ApplicationUserId == userId).Select(x => x.MemberId) .FirstOrDefaultAsync();
        }

        private async Task<bool> IsValidRoomAsync(int roomId)
        {
            return await _context.Rooms.AnyAsync(r => r.RoomId == roomId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ExpenseViewModel viewModel)
        {
            if (!IsValidExpenseViewModel(viewModel))
            {
                return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
            }

            var userId = _userManager.GetUserId(User);
            var memberId = await GetMemberId(userId);

            if (memberId == 0)
            {
                TempData["ErrorMessage"] = "Member not found for the current user.";
                return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
            }

            viewModel.Expense.MemberId = memberId;
            viewModel.Expense.Date = viewModel.Expense.Date.Date;

            try
            {
                _context.Add(viewModel.Expense);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while adding the expense. Please try again.";
                // Log the exception (optional)
                return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
            }

            TempData["SuccessMessage"] = "Expense added successfully.";
            return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExpense(ExpenseViewModel viewModel)
        {
            if (viewModel?.Expense == null || viewModel.RoomId <= 0 || viewModel.Expense.Amount <= 0)
            {
                TempData["ErrorMessage"] = "Invalid input data. Please check all fields and try again.";
                return RedirectToAction("Details", "Rooms", new { id = viewModel?.RoomId ?? 0 });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "User not authenticated.";
                return Unauthorized();
            }

            var expense = await _context.Expenses
                .Where(e => e.ExpenseId == viewModel.Expense.ExpenseId && e.RoomId == viewModel.RoomId)
                .FirstOrDefaultAsync();

            if (expense == null)
            {
                TempData["ErrorMessage"] = "Expense not found or you don't have permission to edit it.";
                return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
            }

            try
            {
                expense.Item = viewModel.Expense.Item?.Trim();
                expense.Amount = viewModel.Expense.Amount;
                expense.Date = viewModel.Expense.Date.Date;
                expense.RoomId = viewModel.RoomId;

                _context.Update(expense);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the expense. Please try again.";
                return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
            }
            TempData["SuccessMessage"] = "Expense updated successfully.";
            return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
        }

        [HttpGet]
        public async Task<IActionResult> DisplayExpenses(int roomId, string month)
        {
            if (roomId <= 0 || !await IsValidRoomAsync(roomId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var selectedMonth))
            {
                return BadRequest("Invalid month format.");
            }

            var expenses = await _context.Expenses.Where(x => x.RoomId == roomId && x.Date.Year == selectedMonth.Year && x.Date.Month == selectedMonth.Month).Include(x => x.Member).ToListAsync();

            if (expenses == null || !expenses.Any())
            {
                return PartialView("_DisplayRoomExpenses", new RoomExpensesViewModel());
            }

            var expensesSummary = expenses
                .GroupBy(x => x.Member.Name)
                .Select(y => new ExpenseSummary
                {
                    MemberName = y.Key,
                    Total = y.Sum(x => x.Amount),
                    Items = y.OrderBy(x => x.Date).ToList()
                }).ToList();

            decimal? total = expenses?.Sum(x => x.Amount);
            int memberCount = await _context.Members.CountAsync(rm => rm.RoomId == roomId);
            decimal? avgAmount = memberCount > 0 ? total / memberCount : 0m;

            var roomExpenseVM = new RoomExpensesViewModel
            {
                Summary = expensesSummary,
                TotalExpense = total,
                AvgPerPerson = avgAmount,
            };

            return PartialView("_DisplayRoomExpenses", roomExpenseVM);
        }
        private bool IsValidExpenseViewModel(ExpenseViewModel viewModel)
        {
            return viewModel != null && viewModel.Expense != null && !string.IsNullOrWhiteSpace(viewModel.Expense.Item) && viewModel.Expense.Amount > 0 && viewModel.RoomId > 0;
        }
    }
}
