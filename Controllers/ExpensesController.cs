// Controllers/ExpensesController.cs
using RoomExpenseTracker.Data;
using RoomExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace RoomExpenseTracker.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly AppDbContext _context;

        public ExpensesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Expenses/Add/5 (RoomId)
        public async Task<IActionResult> Add(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Members)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

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

            // Create view model
            var viewModel = new ExpenseViewModel
            {
                Expense = new Expense { Date = DateTime.Today, RoomId = roomId },
                RoomId = roomId,
                Members = members.Select(m => new SelectListItem
                {
                    Value = m.MemberId.ToString(),
                    Text = m.Name
                }).ToList()
            };

            return PartialView("_AddExpenseModal", viewModel);
        }

        // POST: Expenses/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ExpenseViewModel viewModel)
        {
            if (viewModel!=null && viewModel.Expense!=null && viewModel.Expense.Item != null && viewModel.Expense.Amount > 0 && viewModel.RoomId>0)
            {
                viewModel.Expense.Date = viewModel.Expense.Date.Date;
                _context.Add(viewModel.Expense);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
            }

            // Reload room for validation errors
            var room = await _context.Rooms
                .Include(r => r.Members)
                .FirstOrDefaultAsync(r => r.RoomId == viewModel.RoomId);

            if (room == null)
            {
                return NotFound();
            }

            // Repopulate Members for the view model
            viewModel.Members = (room.Members ?? new List<Member>())
                .Select(m => new SelectListItem
                {
                    Value = m.MemberId.ToString(),
                    Text = m.Name,
                    Selected = m.MemberId == viewModel.Expense.MemberId
                }).ToList();

            return RedirectToAction("Details", "Rooms", new { id = viewModel.RoomId });
        }
    }
}