using Services.Management.AuthService;

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
        public async Task<ApiResponse> CreateRoomAsync(RoomViewModel viewModel)
        {
            string currentUserId = _currentUser.UserId!;

            var existingRoom = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Name!.ToLower() == viewModel.Name.ToLower()
                                       && !r.IsDeleted
                                       && r.Members.Any(m => m.ApplicationUserId == currentUserId));

            if (existingRoom != null)
            {
                return ApiResponse.Fail("You are already a member of a group with this name.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
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

                foreach (var member in room.Members.Where(m => !string.IsNullOrEmpty(m.ApplicationUserId)))
                {
                    _cacheService.ClearRoomsCache(member.ApplicationUserId!);
                }
                return ApiResponse<int>.SuccessRes(room.RoomId, "Room created and invitations processed successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ApiResponse<int>.Fail(0, "An error occurred while creating the room and processing invitations.");
            }
        }

        public async Task<ApiResponse> AddMemberAsync(AddMemberViewModel viewModel)
        {
            string currentUserId = _currentUser.UserId!;

            var room = await _context.Rooms
                .Include(r => r.Members)
                .FirstOrDefaultAsync(r => r.RoomId == viewModel.RoomId && !r.IsDeleted);

            if (room == null)
            {
                return ApiResponse.Fail("Room not found.");
            }

            if (!room.Members.Any(m => m.ApplicationUserId == currentUserId))
            {
                return ApiResponse.Fail("You are not authorized to add members to this room.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingMember = room.Members.FirstOrDefault(m =>
                    m.Email != null && m.Email.Equals(viewModel.Email, StringComparison.OrdinalIgnoreCase));

                if (existingMember != null)
                {
                    return ApiResponse.Fail("This user is already a member of the room.");
                }

                var existingUser = await _userManager.FindByEmailAsync(viewModel.Email!);

                if (existingUser != null)
                {
                    var newMember = new Member
                    {
                        RoomId = room.RoomId,
                        ApplicationUserId = existingUser.Id,
                        Name = viewModel.Name,
                        Email = viewModel.Email,
                        IsPending = false
                    };
                    _context.Members.Add(newMember);
                }
                else
                {
                    var pendingMember = new Member
                    {
                        RoomId = room.RoomId,
                        Name = viewModel.Name,
                        Email = viewModel.Email,
                        IsPending = true
                    };
                    _context.Members.Add(pendingMember);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                foreach (var member in room.Members.Where(m => !string.IsNullOrEmpty(m.ApplicationUserId)))
                {
                    _cacheService.ClearRoomsCache(member.ApplicationUserId!);
                }

                return ApiResponse.SuccessRes("Member added successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ApiResponse.Fail("An error occurred while adding the member.");
            }
        }
        public async Task<ApiResponse> RemoveMemberAsync(int roomId, int memberId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var membership = await _context.Members
                    .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.MemberId == memberId);

                if (membership == null)
                    return ApiResponse.Fail("Member not found in this room.");

                _context.Members.Remove(membership);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var member = await _context.Members.FindAsync(memberId);
                if (member != null && !string.IsNullOrEmpty(member.ApplicationUserId))
                {
                    _cacheService.ClearRoomsCache(member.ApplicationUserId);
                }

                _cacheService.ClearRoomsCache(_currentUser.UserId!);

                return ApiResponse.SuccessRes("Member removed successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ApiResponse.Fail("An error occurred while removing the member.");
            }
        }

        public async Task<ApiResponse> DeleteRoomAsync(int roomId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var room = await _context.Rooms
                    .Include(r => r.Members)
                    .FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null)
                    return ApiResponse.Fail("Room not found.");

                var memberIds = room.Members.Select(m => m.MemberId).ToList();
                var applicationUserIds = await _context.Members
                    .Where(m => memberIds.Contains(m.MemberId) && !string.IsNullOrEmpty(m.ApplicationUserId))
                    .Select(m => m.ApplicationUserId)
                    .ToListAsync();

                _context.Members.RemoveRange(room.Members);
                _context.Rooms.Remove(room);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                foreach (var userId in applicationUserIds)
                {
                    _cacheService.ClearRoomsCache(userId!);
                }

                return ApiResponse.SuccessRes("Room deleted successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ApiResponse.Fail("An error occurred while deleting the room.");
            }
        }
    }
}