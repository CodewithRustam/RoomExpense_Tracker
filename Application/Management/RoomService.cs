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
        private readonly AppDbContext _context;

        public RoomService(
            IUnitOfWork uow,
            AppDbContext context,
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
            _context = context;
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
        public async Task<ApiResponse<int>> CreateRoomAsync(RoomViewModel viewModel, string currentUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Create the new room
                var room = new Room
                {
                    Name = viewModel.Name,
                    CreatedByUserId = currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();

                var creatorUser = await _userManager.FindByIdAsync(currentUserId);

                var creatorMember = new Member
                {
                    RoomId = room.RoomId,
                    ApplicationUserId = currentUserId,
                    Name = creatorUser?.UserName, 
                    Email = creatorUser?.Email,
                    IsPending = false
                };

                _context.Members.Add(creatorMember);

                if (viewModel.Members != null && viewModel.Members.Any())
                {
                    foreach (var invitee in viewModel.Members)
                    {
                        var existingUser = await _userManager.FindByEmailAsync(invitee.Email);

                        if (existingUser != null)
                        {
                            var newMember = new Member
                            {
                                RoomId = room.RoomId,
                                ApplicationUserId = existingUser.Id,
                                Name = invitee.Name,
                                Email = invitee.Email,
                                IsPending = false
                            };
                            _context.Members.Add(newMember);
                        }
                        else
                        {
                            var pendingMember = new Member
                            {
                                RoomId = room.RoomId,
                                Name = invitee.Name,
                                Email = invitee.Email,
                                IsPending = true
                            };
                            _context.Members.Add(pendingMember);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ApiResponse<int>.SuccessRes(room.RoomId, "Room created and invitations processed successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ApiResponse<int>.Fail(0, "An error occurred while creating the room and processing invitations.");
            }
        }
    }
}