namespace ExpenseTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomServices roomServices;
        private readonly ILogger<RoomsController> _logger;
        public RoomsController(IRoomServices roomServices, ILogger<RoomsController> logger)
        {
            this.roomServices = roomServices;
            _logger = logger;
        }

        [HttpGet("get-rooms")]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await roomServices.GetRoomsForCurrentUser();
            var response = ApiResponse<List<RoomResponse>>.SuccessRes(rooms, "Rooms retrieved successfully.");
            return Ok(response);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] RoomViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Invalid room data."));

            var (success, message) = await roomServices.CreateRoomAsync(viewModel);

            if (!success)
                return BadRequest(ApiResponse<string>.Fail(message));

            return Ok(ApiResponse<string>.SuccessRes(null, message));
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(int id, string? month)
        {
            var roomDetails = await roomServices.GetRoomDetails(id, month,false);

            if (roomDetails == null)
                return NotFound(ApiResponse.Fail("Room not found."));

            return Ok(ApiResponse<RoomDetailsViewModel>.SuccessRes(roomDetails, "Room details retrieved successfully."));
        }
    }
}
