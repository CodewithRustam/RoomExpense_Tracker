using FluentValidation;

namespace AppExpenseTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpensesController : ControllerBase
    {
        private readonly IMemberServices _memberServices;
        private readonly IRoomServices _roomServices;
        private readonly IExpenseServices _expenseServices;
        private readonly ISettlementServices _settlementServices;
        private readonly IValidator<ExpenseViewModel> _validator;

        public ExpensesController(
            IMemberServices memberServices,
            IRoomServices roomServices,
            IExpenseServices expenseServices,
            ISettlementServices settlementServices,
            IValidator<ExpenseViewModel> validator)
        {
            _memberServices = memberServices;
            _roomServices = roomServices;
            _expenseServices = expenseServices;
            _settlementServices = settlementServices;
            _validator = validator;
        }

        [HttpGet("get-userexpesne-months")]
        public async Task<IActionResult> GetMonths()
        {
            var months = await _expenseServices.GetExpenseMonthsByUserId();
            return Ok(ApiResponse<IReadOnlyList<string>>.SuccessRes(months, "Months retrieved successfully."));
        }

        [HttpPost("add-expense")]
        public async Task<IActionResult> Add([FromBody] ExpenseViewModel expViewModel)
        {
            var validationError = await GetValidationErrorAsync(expViewModel);
            if (validationError != null)
                return BadRequest(ApiResponse.Fail(validationError));

            ApiResponse apiResponse = await _expenseServices.AddExpenses(expViewModel);
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }

        [HttpPost("update-expense")]
        public async Task<IActionResult> UpdateExpense([FromBody] ExpenseViewModel expenseViewModel)
        {
            var validationError = await GetValidationErrorAsync(expenseViewModel);
            if (validationError != null)
                return BadRequest(ApiResponse.Fail(validationError));

            ApiResponse apiResponse = await _expenseServices.UpdateExpenses(expenseViewModel);
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }

        [HttpDelete("delete-expense/{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            ApiResponse apiResponse = await _expenseServices.DeleteExpense(id);
            return apiResponse.Success ? Ok(apiResponse) : BadRequest(apiResponse);
        }

        [HttpGet("get-room-expenses")]
        public async Task<IActionResult> DisplayExpenses(int roomId, string? month)
        {
            // Fixed REST pattern: returning BadRequest for invalid data instead of 200 OK
            if (roomId <= 0 || !await _roomServices.IsValidRoomAsync(roomId))
                return BadRequest(ApiResponse.Fail("Invalid room ID or room not found."));

            bool includeRoomInfo = string.IsNullOrEmpty(month);
            ApiResponse apiResponse = await _expenseServices.GetRoomExpensesForApi(roomId, month, includeRoomInfo);

            return Ok(apiResponse);
        }

        [HttpGet("get-user-expenses")]
        public async Task<IActionResult> DisplayUserExpenses(string month)
        {
            ApiResponse apiResponse = await _expenseServices.GetUserExpensesForApi(month);
            return Ok(apiResponse);
        }

        [HttpPost("expenses-settle")]
        public async Task<IActionResult> Settle([FromBody] SettlementRequest settlementRequestVM)
        {
            // Security check stays in controller, but correctly returns 401/403 behavior via BadRequest
            if (!User.Identity!.IsAuthenticated || User.Identity.Name != settlementRequestVM.PayerName)
                return BadRequest(ApiResponse.Fail("Unauthorized action: payer name does not match the logged-in user."));

            if (settlementRequestVM.SettlementAmount <= 0)
                return BadRequest(ApiResponse.Fail("Amount must be greater than zero."));

            if (!DateTime.TryParseExact(settlementRequestVM.MonthLabel + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var settlementForMonth))
                return BadRequest(ApiResponse.Fail("Invalid month format."));

            settlementRequestVM.SettlementMonth = settlementForMonth;
            var result = await _settlementServices.SettleExpenseAsync(settlementRequestVM);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Message)); // Fixed to BadRequest

            return Ok(ApiResponse<string>.SuccessRes(null, result.Message));
        }

        [HttpGet("trend-expenses")]
        public async Task<IActionResult> GetMonthlyExpensesTrend(int roomId, string month)
        {
            ApiResponse apiResponse = await _expenseServices.GetMonthlyExpensesTrend(roomId, month);
            return Ok(apiResponse);
        }

        [HttpGet("get-settlement-details")]
        public async Task<IActionResult> GetSettlementDetails(int roomId, int memberId, [FromQuery] string month)
        {
            ApiResponse apiResponse = await _expenseServices.GetSettlementDetails(roomId, memberId, month);
            return Ok(apiResponse);
        }

        [HttpGet("trend-home-expenses")]
        public async Task<IActionResult> GetRoomTrend(int roomId)
        {
            ApiResponse apiResponse = await _expenseServices.GetHomeExpenseTrends(roomId);
            return Ok(apiResponse);
        }

        #region Helpers

        // DRY optimization: Centralized validation parsing
        private async Task<string?> GetValidationErrorAsync(ExpenseViewModel model)
        {
            var validationResult = await _validator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
            }
            return null;
        }

        #endregion
    }
}