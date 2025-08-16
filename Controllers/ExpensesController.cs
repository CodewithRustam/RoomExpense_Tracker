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

        private async Task<int> GetMemberId(string? userId, int roomId)
        {
            return await _context.Members
                .Where(x => x.ApplicationUserId == userId && x.RoomId == roomId)
                .Select(x => x.MemberId)
                .FirstOrDefaultAsync();
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
            var memberId = await GetMemberId(userId, viewModel.RoomId);

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
                .Where(e => e.ExpenseId == viewModel.Expense.ExpenseId && e.RoomId == viewModel.RoomId && (e.IsDeleted == false || e.IsDeleted == null))
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
                return RedirectToAction("AccessDenied", "Account");

            if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var selectedMonth))
                return BadRequest("Invalid month format.");

            // Fetch expenses for the selected month and room
            var expenses = await _context.Expenses
                .Where(x => x.RoomId == roomId && (x.IsDeleted == false || x.IsDeleted == null)
                         && x.Date.Year == selectedMonth.Year
                         && x.Date.Month == selectedMonth.Month)
                .ToListAsync();

            // Fetch settlements for the same period
            var settlements = await _context.Settlements
                .Where(x => x.RoomId == roomId
                         && x.SettlementForDate.Year == selectedMonth.Year
                         && x.SettlementForDate.Month == selectedMonth.Month)
                .ToListAsync();

            // Fetch all members of the room
            var members = await _context.Members
                .Where(m => m.RoomId == roomId)
                .ToListAsync();

            // Calculate overall totals
            var total = expenses.Sum(x => (decimal?)x.Amount) ?? 0m;
            var memberCount = members.Count;
            var avgAmount = memberCount > 0
                ? Math.Round(total / memberCount, 2)
                : 0m;

            var summaries = new List<ExpenseSummary>();
            foreach (var member in members)
            {
                var memberExpenses = expenses.Where(e => e.MemberId == member.MemberId).OrderBy(e => e.Date).ToList();
                var totalExpense = memberExpenses.Sum(e => e.Amount);

                var totalPaid = settlements.Where(s => s.MemberId == member.MemberId).Sum(s => s.Amount);
                var totalReceived = settlements.Where(s => s.PaidToMemberId == member.MemberId).Sum(s => s.Amount);

                var rawDifference = (totalExpense + totalPaid) - totalReceived - avgAmount;
                // Ignore differences under 0.5
                var effectiveDifference = Math.Abs(rawDifference) < 0.5m ? 0m : rawDifference;

                var summary = new ExpenseSummary
                {
                    MemberName = member.Name,
                    TotalExpense = totalExpense,
                    PaidAmount = totalPaid,
                    ReceivedAmount = totalReceived,
                    NetBalance = totalExpense + totalPaid - totalReceived,  // Useful for admin tabular summary
                    Items = memberExpenses,
                    IsOwed = effectiveDifference > 0,
                    IsOwing = effectiveDifference < 0,
                    BadgeText = effectiveDifference > 0 ? "Owed"
                              : effectiveDifference < 0 ? "Owe"
                              : "Settled up",
                    BadgeAmount = effectiveDifference != 0 ? Math.Abs(effectiveDifference) : 0m,
                    RawDifference = effectiveDifference 
                };
                summaries.Add(summary);
            }

            var model = new RoomExpensesViewModel
            {
                Summary = summaries,
                TotalExpense = total,
                AvgPerPerson = avgAmount,
                Expense = new Expense { RoomId = roomId }
            };

            return PartialView("_DisplayRoomExpenses", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settle(int RoomId, string MemberName, string PaidToMemberName, decimal Amount, string Month)
        {
            // 1. Authentication check
            if (!User.Identity.IsAuthenticated || User.Identity.Name != MemberName)
            {
                TempData["ErrorMessage"] = "User not authorized to settle this expense.";
                return Unauthorized();
            }

            // 2. Parse month
            if (!DateTime.TryParseExact(Month + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var settlementForMonth))
            {
                TempData["ErrorMessage"] = "Invalid month format.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            // 3. Validate inputs
            if (string.IsNullOrEmpty(PaidToMemberName))
            {
                TempData["ErrorMessage"] = "Please select a member to settle with.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            if (Amount <= 0)
            {
                TempData["ErrorMessage"] = "Settlement amount must be greater than zero.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            // 4. Get the current user's member record
            var userId = _userManager.GetUserId(User);
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.Name == MemberName && m.RoomId == RoomId && m.ApplicationUserId == userId);

            if (member == null)
            {
                TempData["ErrorMessage"] = "Member not found.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            // 5. Get the recipient's member record
            var paidToMember = await _context.Members
                .FirstOrDefaultAsync(m => m.Name == PaidToMemberName && m.RoomId == RoomId);

            if (paidToMember == null)
            {
                TempData["ErrorMessage"] = "Recipient member not found.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            // 6. Validate room existence
            var roomExists = await _context.Rooms.AnyAsync(r => r.RoomId == RoomId);
            if (!roomExists)
            {
                TempData["ErrorMessage"] = "Room not found.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            // 7. Calculate balances for the specified month
            var startOfMonth = settlementForMonth;
            var endOfMonth = settlementForMonth.AddMonths(1).AddTicks(-1);

            var totalExpenses = await _context.Expenses
                .Where(e => e.RoomId == RoomId && e.MemberId == member.MemberId
                    && (e.IsDeleted == false || e.IsDeleted == null)
                    && e.Date >= startOfMonth && e.Date <= endOfMonth)
                .SumAsync(e => e.Amount);

            var totalSettlementsPaid = await _context.Settlements
                .Where(s => s.RoomId == RoomId && s.MemberId == member.MemberId
                    && s.SettlementForDate >= startOfMonth && s.SettlementForDate <= endOfMonth)
                .SumAsync(s => s.Amount);

            var totalSettlementsReceived = await _context.Settlements
                .Where(s => s.RoomId == RoomId && s.PaidToMemberId == member.MemberId
                    && s.SettlementForDate >= startOfMonth && s.SettlementForDate <= endOfMonth)
                .SumAsync(s => s.Amount);

            var paidToTotalExpenses = await _context.Expenses
                .Where(e => e.RoomId == RoomId && e.MemberId == paidToMember.MemberId
                    && (e.IsDeleted == false || e.IsDeleted == null)
                    && e.Date >= startOfMonth && e.Date <= endOfMonth)
                .SumAsync(e => e.Amount);

            var paidToSettlementsPaid = await _context.Settlements
                .Where(s => s.RoomId == RoomId && s.MemberId == paidToMember.MemberId
                    && s.SettlementForDate >= startOfMonth && s.SettlementForDate <= endOfMonth)
                .SumAsync(s => s.Amount);

            var paidToSettlementsReceived = await _context.Settlements
                .Where(s => s.RoomId == RoomId && s.PaidToMemberId == paidToMember.MemberId
                    && s.SettlementForDate >= startOfMonth && s.SettlementForDate <= endOfMonth)
                .SumAsync(s => s.Amount);

            var totalRoomExpenses = await _context.Expenses
                .Where(e => e.RoomId == RoomId && (e.IsDeleted == false || e.IsDeleted == null)
                    && e.Date >= startOfMonth && e.Date <= endOfMonth)
                .SumAsync(e => e.Amount);

            var memberCount = await _context.Members
                .CountAsync(m => m.RoomId == RoomId);

            var avgPerPerson = memberCount > 0 ? totalRoomExpenses / memberCount : 0;

            // 8. Calculate current balances
            var payerBalance = totalExpenses - totalSettlementsPaid + totalSettlementsReceived;
            var payerOwedAmount = Math.Abs(payerBalance - avgPerPerson); // Amount the payer owes
            var recipientBalance = paidToTotalExpenses - paidToSettlementsPaid + paidToSettlementsReceived;
            var recipientOwedAmount = recipientBalance - avgPerPerson; // Amount the recipient is owed

            // 9. Validate settlement amount
            if (payerBalance >= avgPerPerson)
            {
                TempData["ErrorMessage"] = "You are not owing any amount for this month.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            if (recipientBalance <= avgPerPerson)
            {
                TempData["ErrorMessage"] = "The selected member is not owed any amount for this month.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            // 10. Ensure settlement amount doesn't exceed what's owed
            var maxSettlementAmount = Math.Min(payerOwedAmount, Math.Abs(recipientOwedAmount));
            if (Amount > maxSettlementAmount)
            {
                TempData["ErrorMessage"] = $"Settlement amount cannot exceed ₹{maxSettlementAmount:F2}.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            // 11. Record the settlement
            var settlement = new Settlement
            {
                MemberId = member.MemberId,
                PaidToMemberId = paidToMember.MemberId,
                RoomId = RoomId,
                Amount = Amount,
                SettlementDate = DateTime.Now,
                SettlementForDate = settlementForMonth 
            };

            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    _context.Settlements.Add(settlement);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "An error occurred while recording the settlement. Please try again.";
                return RedirectToAction("Details", "Rooms", new { id = RoomId });
            }

            TempData["SuccessMessage"] = $"Successfully settled ₹{Amount} to {PaidToMemberName} for {Month}.";
            return RedirectToAction("Details", "Rooms", new { id = RoomId });
        }
        private bool IsValidExpenseViewModel(ExpenseViewModel viewModel)
        {
            return viewModel != null && viewModel.Expense != null && !string.IsNullOrWhiteSpace(viewModel.Expense.Item) && viewModel.Expense.Amount > 0 && viewModel.RoomId > 0;
        }
    }
}