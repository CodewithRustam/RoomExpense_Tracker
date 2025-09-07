using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;

namespace ExpenseTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomServices roomServices;

        public RoomsController(IRoomServices roomServices)
        {
            this.roomServices = roomServices;
        }

        [HttpGet("get-rooms")]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await roomServices.GetRoomsForCurrentUser();
            return Ok(ApiResponse<object>.Ok(rooms, "Rooms retrieved successfully."));
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] RoomViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<string>.Fail("Invalid room data."));

            var (success, message) = await roomServices.CreateRoomAsync(viewModel);

            if (!success)
                return BadRequest(ApiResponse<string>.Fail(message));

            return Ok(ApiResponse<string>.Ok(null, message));
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(int id, string? month)
        {
            var roomDetails = new Object();//await roomServices.GetRoomDetails(id, month);

            if (roomDetails == null)
                return NotFound(ApiResponse<string>.Fail("Room not found."));

            return Ok(ApiResponse<object>.Ok(roomDetails, "Room details retrieved successfully."));
        }
    }
}
