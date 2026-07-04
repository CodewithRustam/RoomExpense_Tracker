namespace Services.Management
{
    public class RoomService : IRoomServices
    {
        private readonly IUnitOfWork _uow;
        private readonly IRoomRepository _roomRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IRoomMapper _mapper;
        private readonly IRoomCacheService _cacheService;

        public RoomService(
            IUnitOfWork uow,
            IRoomRepository roomRepository,
            IMemberRepository memberRepository,
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager,
            IRoomMapper mapper,
            IRoomCacheService cacheService)
        {
            _uow = uow;
            _roomRepository = roomRepository;
            _memberRepository = memberRepository; 
            _currentUser = currentUser;
            _userManager = userManager;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<List<RoomResponse>> GetRoomsForCurrentUser()
        {
            string? userId = _currentUser.UserId;

            if (_cacheService.TryGetRooms(userId!, out var cachedRooms))
                return cachedRooms!.ToList();

            var rooms = await _roomRepository.GetRoomsForCurrentUser(userId);

            var roomResponses = _mapper.MapToRoomResponses(rooms).ToList();

            _cacheService.SetRooms(userId!, roomResponses);

            return roomResponses;
        }

        public async Task<bool> IsValidRoomAsync(int roomId)
        {
            return await _roomRepository.IsValidRoomAsync(roomId);
        }

        public async Task<RoomDetailsViewModel?> GetRoomDetails(int roomId, string? month, bool isFromSettled)
        {
            var room = await _roomRepository.GetRoomDetails(roomId, _currentUser.UserId);

            return _mapper.MapToRoomDetailsViewModel(room, month, isFromSettled);
        }

        public async Task<(bool success, string message)> CreateRoomAsync(RoomViewModel viewModel)
        {
            string? userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId)) return (false, RoomMessages.UserNotFound);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.UserName)) return (false, RoomMessages.InvalidUser);

            var room = new Room
            {
                Name = viewModel.Name,
                CreatedByUserId = userId,
                Members = new List<Member>
                {
                    new Member
                    {
                        Name = user.UserName,
                        ApplicationUserId = userId
                    }
                }
            };

            // Process additional members
            var requestedUsernames = viewModel.MemberUserNames
                .Where(u => !string.IsNullOrWhiteSpace(u) && u != user.UserName)
                .Distinct()
                .ToList();

            if (requestedUsernames.Any())
            {
                foreach (var username in requestedUsernames)
                {
                    var existingUser = await _userManager.FindByNameAsync(username);
                    if (existingUser == null)
                        return (false, RoomMessages.UserDoesNotExist(username));

                    room.Members.Add(new Member
                    {
                        Name = username,
                        ApplicationUserId = existingUser.Id
                    });
                }
            }

            await _roomRepository.AddAsync(room);

            if (await _uow.SaveAsync() > 0)
            {
                _cacheService.ClearRoomsCache(userId);
                return (true, RoomMessages.RoomCreated);
            }

            return (false, RoomMessages.RoomCreationFailed);
        }
    }
}