using System.Security.Claims;
using Services.ViewModels;

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

            var response = await _roomServices.CreateRoomAsync(viewModel);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(int id, [FromQuery] string? month)
        {
            var roomDetails = await _roomServices.GetRoomDetails(id, month, false);

            if (roomDetails == null)
                return NotFound(ApiResponse.Fail("Room not found."));

            return Ok(ApiResponse<RoomDetailsViewModel>.SuccessRes(roomDetails, "Room details retrieved successfully."));
        }
        [HttpPost("add-member")]
        public async Task<IActionResult> AddMember([FromBody] AddMemberViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid member data."));

            var response = await _roomServices.AddMemberAsync(viewModel);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
        [HttpDelete("remove-member/{roomId}/{memberId}")]
        public async Task<IActionResult> RemoveMember(int roomId, int memberId)
        {
            var response = await _roomServices.RemoveMemberAsync(roomId, memberId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [HttpDelete("delete/{roomId}")]
        public async Task<IActionResult> DeleteGroup(int roomId)
        {
            var response = await _roomServices.DeleteRoomAsync(roomId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("invitation/{token}")]
        public async Task<IActionResult> GetInvitation(string token)
        {
            var response = await _roomServices.GetInvitationDetailsAsync(token);
            if (!response.Success)
                return NotFound(response);

            return Ok(response);
        }

        [HttpPost("accept-invitation")]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request?.Token))
                return BadRequest(ApiResponse.Fail("Invalid invitation request."));

            var userId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("You must be logged in to accept this invitation."));

            var response = await _roomServices.AcceptInvitationAsync(request.Token, userId);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }
    }
}