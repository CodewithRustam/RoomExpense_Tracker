using Azure;
using FirebaseAdmin.Messaging;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Management;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;
using System.Globalization;
using static Google.Apis.Requests.BatchRequest;

namespace AppExpenseTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpensesController : ControllerBase
    {
        private readonly IMemberServices memberServices;
        private readonly IRoomServices roomServices;
        private readonly IExpenseServices expenseServices;
        private readonly ISettlementServices settlementServices;

        public ExpensesController(IMemberServices memberServices,
            IRoomServices roomServices,
            IExpenseServices expenseServices,
            ISettlementServices settlementServices)
        {
            this.memberServices = memberServices;
            this.roomServices = roomServices;
            this.expenseServices = expenseServices;
            this.settlementServices = settlementServices;
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] ExpenseViewModel expViewModel)
        {
            if (expViewModel is null)
                return BadRequest(ApiResponse.Fail("Invalid expense data."));

            var memberId = await memberServices.GetMemberId(expViewModel.RoomId);
            if (memberId == 0)
                return BadRequest(ApiResponse.Fail("Member not found."));

            expViewModel.MemberId = memberId;

            var message = await expenseServices.AddExpenses(expViewModel);

            return Ok(ApiResponse.Ok(message));
        }

        [HttpPost("update-expense")]
        public async Task<IActionResult> Edit([FromBody] ExpenseViewModel viewModel)
        {
            if (viewModel is null)
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));

            var message = await expenseServices.UpdateExpenses(viewModel);
            return Ok(ApiResponse<string>.Ok(null, message));
        }

        [HttpGet("display-room-expense")]
        public async Task<IActionResult> DisplayExpenses(int roomId, string? month)
        {
            if (roomId <= 0 || !await roomServices.IsValidRoomAsync(roomId))
                return Ok(ApiResponse.Fail("Invalid room."));

            RoomExpenseResponse roomExpenseRes;
            if (string.IsNullOrEmpty(month))
            {
                roomExpenseRes = await expenseServices.GetRoomExpensesForApi(roomId, new DateTime());
            }
            else
            {
                if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var selectedMonth))
                    return Ok(ApiResponse.Fail("Invalid month format."));

                roomExpenseRes = await expenseServices.GetRoomExpensesForApi(roomId, selectedMonth, false);
            }
            return Ok(ApiResponse<RoomExpenseResponse>.Ok(roomExpenseRes, "Monthly expenses retrieved."));
        }
        [HttpGet("display-user-expense")]
        public async Task<IActionResult> DisplayUserExpenses()
        {
            var result = await expenseServices.GetUserExpensesForApi();
            return Ok(ApiResponse<UserExpenseDetails>.Ok(result, "User expenses retrieved."));
        }

        [HttpPost("expenses-settle")]
        public async Task<IActionResult> Settle([FromBody] SettlementRequest settlementRequestVM)
        {
            if (!User.Identity!.IsAuthenticated || User.Identity.Name != settlementRequestVM.PayerName)
                return Ok(ApiResponse.Fail("Unauthorized action: payer name does not match the logged-in user."));

            if (!DateTime.TryParseExact(settlementRequestVM.MonthLabel + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var settlementForMonth))
                return Ok(ApiResponse.Fail("Invalid month format."));

            if (settlementRequestVM.SettlementAmount <= 0)
                return Ok(ApiResponse.Fail("Amount must be greater than zero."));

            settlementRequestVM.SettlementMonth = settlementForMonth;
            var result = await settlementServices.SettleExpenseAsync(settlementRequestVM);

            if (!result.Success)
                return Ok(ApiResponse.Fail(result.Message));

            return Ok(ApiResponse<string>.Ok(null,result.Message));
        }
        [HttpGet("expenses-trend")]
        public async Task<IActionResult> GetMonthlyExpensesTrend(int roomId, string month)
        {
            var membersRecord = await expenseServices.GetMonthlyExpensesTrend(roomId, month);
            return Ok(membersRecord);
        }
        [HttpGet("settlements")]
        public async Task<IActionResult> GetSettlementDetails(int roomId, int memberId, [FromQuery] string? month = null)
        {
            if (roomId <= 0)
            {
                return Ok(ApiResponse.Fail("Invalid room ID."));
            }

            if (memberId <= 0)
            {
                return Ok(ApiResponse.Fail("Invalid member ID."));
            }

            DateTime? targetMonth = null;
            if (!string.IsNullOrEmpty(month))
            {
                if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedMonth))
                {
                    return Ok(ApiResponse.Fail("Invalid month format. Use YYYY-MM."));
                }
                targetMonth = parsedMonth;
            }

            var result = await expenseServices.GetSettlementDetails(roomId, memberId, targetMonth);

            return Ok(ApiResponse<SettlementData>.Ok(result));

        }
    }
}
