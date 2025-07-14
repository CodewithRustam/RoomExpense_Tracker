using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomExpenseTracker.Data;
using RoomExpenseTracker.Models;
using RoomExpenseTracker.Models.AppUser;

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

        public async Task<IActionResult> Add(int roomId)
        {
            var room = await _context.Rooms.Include(r => r.Members).FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
            {
                TempData["Error"] = "Room not found.";
                return RedirectToAction("Index", "Rooms");
            }

            var members = room.Members ?? new List<Member>();

            if (!members.Any())
            {
                TempData["Error"] = "No members found in this room. Please add members first.";
                return RedirectToAction("Details", "Rooms", new { id = roomId });
            }

            var viewModel = new ExpenseViewModel
            {
                Expense = new Expense { Date = DateTime.Today, RoomId = roomId },
                RoomId = roomId,
            };

            return PartialView("_AddExpenseModal", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ExpenseViewModel viewModel)
        {
            if (viewModel!=null && viewModel.Expense!=null && viewModel.Expense.Item != null && viewModel.Expense.Amount > 0 && viewModel.RoomId>0)
            {
                var userId = _userManager.GetUserId(User);

                viewModel.Expense.MemberId = _context.Members.Where(x => x.ApplicationUserId == userId).Select(x => x.MemberId).FirstOrDefault();
                viewModel.Expense.Date = viewModel.Expense.Date.Date;
                _context.Add(viewModel.Expense);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
            }

            var room = await _context.Rooms.Include(r => r.Members).FirstOrDefaultAsync(r => r.RoomId == viewModel.RoomId);

            if (room == null)
            {
                return NotFound();
            }

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

            var expense = await _context.Expenses.Include(e => e.Room).FirstOrDefaultAsync(e => e.ExpenseId == viewModel.Expense.ExpenseId &&
                                        e.Room.Members.Any(m => m.ApplicationUserId == userId));

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
                TempData["SuccessMessage"] = "Expense updated successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the expense. Please try again.";
                return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
            }

            return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
        }
    }
}