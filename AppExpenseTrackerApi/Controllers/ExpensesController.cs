using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;
using System.Globalization;

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
                return BadRequest(ApiResponse<string>.Fail("Member not found."));

            expViewModel.MemberId = memberId;

            var message = await expenseServices.AddExpenses(expViewModel);

            return Ok(ApiResponse<string>.Ok(null, message));
        }

        [HttpPost("edit")]
        public async Task<IActionResult> Edit([FromBody] ExpenseViewModel viewModel)
        {
            if (viewModel is null)
                return BadRequest(ApiResponse<string>.Fail("Invalid data."));

            var message = await expenseServices.UpdateExpenses(viewModel);
            return Ok(ApiResponse<string>.Ok(null, message));
        }

        [HttpGet("display-expense")]
        public async Task<IActionResult> DisplayExpenses(int roomId, string? month)
        {
            if (roomId <= 0 || !await roomServices.IsValidRoomAsync(roomId))
                return Unauthorized(ApiResponse.Fail("Invalid room."));

            RoomExpenseResponse roomExpenseRes;
            if (string.IsNullOrEmpty(month))
            {
                roomExpenseRes = await expenseServices.GetRoomExpensesForApi(roomId, new DateTime());
            }
            else
            {

                if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var selectedMonth))
                    return BadRequest(ApiResponse.Fail("Invalid month format."));

                roomExpenseRes = await expenseServices.GetRoomExpensesForApi(roomId, selectedMonth, false);
            }
            return Ok(ApiResponse<RoomExpenseResponse>.Ok(roomExpenseRes, "Monthly expenses retrieved."));
        }
        [HttpGet("display-user-expense")]
        public async Task<IActionResult> DisplayUserExpenses()
        {
            var result = await expenseServices.GetUserExpensesForApi();
            return Ok(ApiResponse<List<UserExpenseResponse>>.Ok(result, "User expenses retrieved."));
        }

        [HttpPost("settle")]
        public async Task<IActionResult> Settle([FromBody] SettlementRequest model)
        {
            if (!User.Identity!.IsAuthenticated || User.Identity.Name != model.MemberName)
                return Unauthorized(ApiResponse<string>.Fail("User not authorized."));

            if (!DateTime.TryParseExact(model.Month + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var settlementForMonth))
                return BadRequest(ApiResponse<string>.Fail("Invalid month format."));

            if (model.Amount <= 0)
                return BadRequest(ApiResponse<string>.Fail("Amount must be greater than zero."));

            var result = await settlementServices.SettleExpenseAsync(model.RoomId, model.MemberName, model.PaidToMemberName, model.Amount, settlementForMonth);

            if (!result.Success)
                return BadRequest(ApiResponse<string>.Fail(result.Message));

            return Ok(ApiResponse<string>.Ok(null, result.Message));
        }
    }

    public class SettlementRequest
    {
        public int RoomId { get; set; }
        public string MemberName { get; set; } = "";
        public string PaidToMemberName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Month { get; set; } = "";
    }
}
