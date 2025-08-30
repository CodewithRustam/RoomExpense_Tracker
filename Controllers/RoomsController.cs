using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppExpenseTracker.ViewModels;
using Services.Interfaces;

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
            //if (!ModelState.IsValid) return View(viewModel);

            //var userId = _userManager.GetUserId(User);
            //var user = await _userManager.GetUserAsync(User);

            //var room = new Room
            //{
            //    CreatedByUser = user,
            //    Name = viewModel.Name,
            //    CreatedByUserId = userId
            //};

            //_context.Rooms.Add(room);
            //await _context.SaveChangesAsync();

            //_context.Members.Add(new Member
            //{
            //    Name = user.UserName,
            //    RoomId = room.RoomId,
            //    ApplicationUserId = userId
            //});

            //foreach (var username in viewModel.MemberUserNames.Where(u => !string.IsNullOrWhiteSpace(u)))
            //{
            //    var existingUser = await _userManager.FindByNameAsync(username);

            //    _context.Members.Add(new Member
            //    {
            //        Name = username,
            //        RoomId = room.RoomId,
            //        ApplicationUserId = existingUser?.Id
            //    });
            //}

            //await _context.SaveChangesAsync();

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