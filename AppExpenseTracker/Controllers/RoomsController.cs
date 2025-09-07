using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.ViewModels;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class RoomsController : Controller
    {
        private readonly IRoomServices roomServices;
        public RoomsController(IRoomServices _roomServices)
        {
            roomServices = _roomServices;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rooms = await roomServices.GetRoomsForCurrentUser();
            return View(rooms);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);

            var (success, message) = await roomServices.CreateRoomAsync(viewModel);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View(viewModel);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, string? month, bool isFromSettled = false)
        {
            var roomDetails = await roomServices.GetRoomDetails(id,month,isFromSettled);
           
            return View(roomDetails);
        }
    }

}