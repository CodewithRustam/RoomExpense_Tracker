namespace Services.Management
{
    public class RoomService : IRoomServices
    {
        private readonly IRoomRepository roomRepository;
        private readonly ICurrentUserService currentUser;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache cache;

        public RoomService(IRoomRepository _roomRepository, ICurrentUserService _currentUser, UserManager<ApplicationUser> userManager, IMemoryCache _cache) 
        {
            roomRepository = _roomRepository;
            currentUser = _currentUser;
            _userManager = userManager;
            cache = _cache;
        }

        public async Task<List<RoomResponse>> GetRoomsForCurrentUser()
        {
            string? userId = currentUser.UserId;
            string cacheKey = CacheHelper.GetRoomsUserKey(userId);

            if (cache.TryGetValue(cacheKey, out List<RoomResponse>? cachedRooms))
            {
                return cachedRooms ?? new List<RoomResponse>();
            }

            var rooms = await roomRepository.GetRoomsForCurrentUser(userId);

            var roomResponses = rooms.Select(r =>
            {
                if (r.Expenses == null || !r.Expenses.Any())
                {
                    return new RoomResponse
                    {
                        RoomId = r.RoomId,
                        Name = r.Name ?? string.Empty,
                        CreatedByUserId = r.CreatedByUserId,
                        CreatedDate = r.CreatedDate,
                        MemberNames = string.Join(", ", r.Members.Select(m => m.Name)),
                        TotalAmount = 0,
                        Type = "Private",
                        IconName = string.Empty,
                        Status = r.IsDeleted ? "Deleted" : "Active"
                    };
                }

                var lastExpenseDate = r.Expenses.Max(e => e.Date);
                int targetMonth = lastExpenseDate.Month;
                int targetYear = lastExpenseDate.Year;

                var totalAmount = r.Expenses
                    .Where(e => e.Date.Month == targetMonth && e.Date.Year == targetYear && (e.IsDeleted == false || e.IsDeleted == null))
                    .Sum(e => e.Amount);

                return new RoomResponse
                {
                    RoomId = r.RoomId,
                    Name = r.Name ?? string.Empty,
                    CreatedByUserId = r.CreatedByUserId,
                    CreatedDate = r.CreatedDate,
                    MemberNames = string.Join(", ", r.Members.Select(m => m.Name)),
                    TotalAmount = totalAmount,
                    Type = "Private",
                    IconName = string.Empty,
                    Status = r.IsDeleted ? "Deleted" : "Active",
                    Month = new DateTime(targetYear, targetMonth, 1).ToString("yyyy-MMM")
                };
            }).ToList();

            cache.Set(cacheKey, roomResponses, TimeSpan.FromDays(30));
            return roomResponses;
        }
        public async Task<bool> IsValidRoomAsync(int roomId)
        {
           return await roomRepository.IsValidRoomAsync(roomId);
        }
        public async Task<RoomDetailsViewModel?> GetRoomDetails(int roomId, string? month, bool isFromSettled)
        {
            string? userId = currentUser.UserId;
            var room = await roomRepository.GetRoomDetails(roomId, userId);

            if (room == null)
                return null;

            var months = room.Expenses.Select(e => e.Date.ToString("yyyy-MM")).Distinct().OrderByDescending(m => m).ToList();

            return new RoomDetailsViewModel
            {
                Room = room,
                AvailableMonths = months,
                SelectedMonth = month ?? months.FirstOrDefault(),
                IsFromSettled = isFromSettled
            };
        }
        public async Task<(bool success, string message)> CreateRoomAsync(RoomViewModel viewModel)
        {
            string? userId = currentUser.UserId;
            if (string.IsNullOrEmpty(userId)) return (false, "User not found.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.UserName)) return (false, "Invalid user.");

            var room = new Room
            {
                Name = viewModel.Name,
                CreatedByUserId = userId
            };

            await roomRepository.AddAsync(room);

            var members = new List<Member>
                              {
                                  new Member
                                  {
                                      Name = user.UserName,
                                      RoomId = room.RoomId,
                                      ApplicationUserId = userId
                                  }
                              };

            foreach (var username in viewModel.MemberUserNames.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                var existingUser = await _userManager.FindByNameAsync(username);
                if (existingUser == null)
                    return (false, $"User {username} does not exist.");

                if (!await roomRepository.MemberExistsAsync(room.RoomId, username))
                {
                    members.Add(new Member
                    {
                        Name = username,
                        RoomId = room.RoomId,
                        ApplicationUserId = existingUser.Id
                    });
                }
            }

            await roomRepository.AddMembersAsync(members);

            return (true, "Room created successfully.");
        }
    }
}
