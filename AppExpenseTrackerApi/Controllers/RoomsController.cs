namespace ExpenseTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomServices _roomServices;
        private readonly ILogger<RoomsController> _logger;

        public RoomsController(IRoomServices roomServices, ILogger<RoomsController> logger)
        {
            _roomServices = roomServices;
            _logger = logger;
        }

        [HttpGet("get-rooms")]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await _roomServices.GetRoomsForCurrentUser();
            var response = ApiResponse<List<RoomResponse>>.SuccessRes(rooms, "Rooms retrieved successfully.");

            return Ok(response);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] RoomViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid room data."));

            var (success, message) = await _roomServices.CreateRoomAsync(viewModel);

            if (!success)
                return BadRequest(ApiResponse.Fail(message));

            return Ok(ApiResponse.SuccessRes(message));
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(int id, [FromQuery] string? month)
        {
            var roomDetails = await _roomServices.GetRoomDetails(id, month, false);

            if (roomDetails == null)
                return NotFound(ApiResponse.Fail("Room not found."));

            return Ok(ApiResponse<RoomDetailsViewModel>.SuccessRes(roomDetails, "Room details retrieved successfully."));
        }
    }
}