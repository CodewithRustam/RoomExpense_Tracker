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
        [HttpGet("get-userexpesne-months")]
        public async Task<IActionResult> GetMonths(int roomId)
        {
            var months = await expenseServices.GetExpenseMonthsByUserId();
            return Ok(ApiResponse<List<string>>.SuccessRes(months, "Months retrieved successfully."));
        }
        [HttpPost("add-expense")]
        public async Task<IActionResult> Add([FromBody] ExpenseViewModel expViewModel)
        {
            ApiResponse apiResponse = await expenseServices.AddExpenses(expViewModel);
            return Ok(apiResponse);
        }

        [HttpPost("update-expense")]
        public async Task<IActionResult> UpdateExpense([FromBody] ExpenseViewModel expenseViewModel)
        {
            ApiResponse apiResponse = await expenseServices.UpdateExpenses(expenseViewModel);
            return Ok(apiResponse);
        }

        [HttpGet("get-room-expenses")]
        public async Task<IActionResult> DisplayExpenses(int roomId, string? month)
        {
            if (roomId <= 0 || !await roomServices.IsValidRoomAsync(roomId))
                return Ok(ApiResponse.Fail("Invalid room ID or room not found."));

            ApiResponse apiResponse;
            if (string.IsNullOrEmpty(month))
            {
                apiResponse = await expenseServices.GetRoomExpensesForApi(roomId, month);
            }
            else
            {
                apiResponse = await expenseServices.GetRoomExpensesForApi(roomId, month, false);
            }
            return Ok(apiResponse);
        }
        [HttpGet("get-user-expenses")]
        public async Task<IActionResult> DisplayUserExpenses(string month)
        {
            ApiResponse apiResponse = await expenseServices.GetUserExpensesForApi(month);
            return Ok(apiResponse);
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

            return Ok(ApiResponse<string>.SuccessRes(null,result.Message));
        }
        [HttpGet("trend-expenses")]
        public async Task<IActionResult> GetMonthlyExpensesTrend(int roomId, string month)
        {
            ApiResponse apiResponse = await expenseServices.GetMonthlyExpensesTrend(roomId, month);
            return Ok(apiResponse);
        }
        [HttpGet("get-settlement-details")]
        public async Task<IActionResult> GetSettlementDetails(int roomId, int memberId, [FromQuery] string month)
        {
            ApiResponse apiResponse = await expenseServices.GetSettlementDetails(roomId, memberId, month);
            return Ok(apiResponse);
        }
    }
}
