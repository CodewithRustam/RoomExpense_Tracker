using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;
using System.Globalization;

namespace AppExpenseTracker.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly IMemberServices memberServices;
        private readonly IRoomServices roomServices;
        private readonly IExpenseServices expenseServices;
        private readonly ISettlementServices settlementServices;
        private readonly IMemoryCache cache;
        public ExpensesController(IMemberServices _memberServices, IRoomServices _roomServices, IMemoryCache _cache, IExpenseServices _expenseServices, ISettlementServices _settlementServices)
        {
            memberServices = _memberServices;
            roomServices = _roomServices;
            cache = _cache;
            expenseServices = _expenseServices;
            settlementServices = _settlementServices;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ExpenseViewModel viewModel)
        {
            string message = string.Empty;
            int roomId = 0;
            try
            {
                if (viewModel is not null && viewModel.Expense is not null)
                {
                    roomId = viewModel.RoomId;
                    var memberId = await memberServices.GetMemberId(roomId);
                    if (memberId == 0)
                    {
                        message = "Member not found for the current user.";
                    }

                    viewModel.Expense.MemberId = memberId;
                    viewModel.Expense.Date = viewModel.Expense.Date.Date;

                    message = await expenseServices.AddExpenses(viewModel);
                }
            }
            catch (Exception)
            {
                throw;
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction("Details", "Rooms", new { id = roomId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExpense(ExpenseViewModel viewModel)
        {
            int Roomid = 0;
            string message = string.Empty;
            try
            {
                if (viewModel is not null)
                {
                    Roomid = viewModel.RoomId;
                   message = await expenseServices.UpdateExpenses(viewModel);
                }
            }
            catch (Exception)
            {
                throw;
            }
            TempData["SuccessMessage"] = message;
            return RedirectToAction("Details", "Rooms", new { id = Roomid });
        }

        [HttpGet]
        public async Task<IActionResult> DisplayExpenses(int roomId, string month)
        {
            if (roomId <= 0 || !await roomServices.IsValidRoomAsync(roomId))
                return RedirectToAction("AccessDenied", "Account");

            if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var selectedMonth))
                return BadRequest("Invalid month format.");

            RoomExpensesViewModel roomExpensesViewModel = await expenseServices.GetMonthlyExpenses(roomId, selectedMonth);

            return PartialView("_DisplayRoomExpenses", roomExpensesViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settle(int RoomId, string MemberName, string PaidToMemberName, decimal Amount, string Month)
        {
            try
            {
                if (!User.Identity!.IsAuthenticated || User.Identity.Name != MemberName)
                    return Unauthorized("User not authorized.");

                if (!DateTime.TryParseExact(Month + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var settlementForMonth))
                {
                    TempData["ErrorMessage"] = "Invalid month format.";
                    return RedirectToAction("Details", "Rooms", new { id = RoomId, month = Month });
                }

                if (Amount <= 0)
                {
                    TempData["ErrorMessage"] = "Amount must be greater than zero.";
                    return RedirectToAction("Details", "Rooms", new { id = RoomId, month = Month });
                }

                var result = await settlementServices.SettleExpenseAsync(RoomId, MemberName, PaidToMemberName, Amount, settlementForMonth);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Details", "Rooms", new { id = RoomId, month = Month });
                }

                TempData["SuccessMessage"] = result.Message;

                return RedirectToAction("Details", "Rooms", new { id = RoomId, month = Month });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}