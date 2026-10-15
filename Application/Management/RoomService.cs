using Services.Management.AuthService;
using Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using ExpenseTrakcerHepler;
using Domain.Entities;

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
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly IEmailTemplateService _emailTemplateService;

        public RoomService(
            IUnitOfWork uow,
            AppDbContext context,
            IRoomRepository roomRepository,
            IMemberRepository memberRepository,
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager,
            IRoomMapper mapper,
            IRoomCacheService cacheService,
            IEmailSender emailSender,
            IConfiguration configuration,
            IEmailTemplateService emailTemplateService)
        {
            _uow = uow;
            _roomRepository = roomRepository;
            _memberRepository = memberRepository; 
            _currentUser = currentUser;
            _userManager = userManager;
            _mapper = mapper;
            _cacheService = cacheService;
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
            _emailTemplateService = emailTemplateService;
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
                    CreatedDate = DateTimeProvider.NowIST,
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
                    IsPending = false,
                    JoinedDate = DateTimeProvider.NowIST,
                    CreatedDate = DateTimeProvider.NowIST,
                    CreatedByUserId = currentUserId
                };

                _context.Members.Add(creatorMember);

                var invitationsToSend = new List<(string Email, string Name, string Token)>();

                if (viewModel.Members != null && viewModel.Members.Any())
                {
                    foreach (var invitee in viewModel.Members)
                    {
                        if (string.IsNullOrWhiteSpace(invitee.Email)) continue;

                        var normalizedEmail = invitee.Email.Trim().ToLowerInvariant();
                        var inviteeName = string.IsNullOrWhiteSpace(invitee.Name) ? "Friend" : invitee.Name.Trim();
                        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

                        var pendingMember = new Member
                        {
                            RoomId = room.RoomId,
                            ApplicationUserId = existingUser?.Id,
                            Name = inviteeName,
                            Email = normalizedEmail,
                            IsPending = true,
                            JoinedDate = DateTimeProvider.NowIST,
                            CreatedDate = DateTimeProvider.NowIST,
                            CreatedByUserId = currentUserId
                        };
                        _context.Members.Add(pendingMember);

                        var token = Guid.NewGuid().ToString("N");
                        var invitation = new RoomInvitation
                        {
                            RoomId = room.RoomId,
                            InvitedEmail = normalizedEmail,
                            InvitedName = inviteeName,
                            Token = token,
                            Status = InvitationStatus.Pending,
                            ExpiresAt = DateTimeProvider.NowIST.AddDays(7),
                            CreatedDate = DateTimeProvider.NowIST,
                            CreatedByUserId = currentUserId
                        };
                        _context.RoomInvitations.Add(invitation);

                        invitationsToSend.Add((normalizedEmail, inviteeName, token));
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _cacheService.ClearRoomsCache(currentUserId);

                // Dispatch invitation emails asynchronously
                var baseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "https://splitx-exp.netlify.app";
                var inviterName = creatorUser?.UserName ?? "A friend";

                foreach (var item in invitationsToSend)
                {
                    try
                    {
                        var joinUrl = $"{baseUrl}/join-room?token={item.Token}";
                        var (subject, body) = await _emailTemplateService.GetRoomInvitationEmailAsync(inviterName, room.Name, joinUrl, item.Name);
                        await _emailSender.SendEmailAsync(item.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Failed to send invitation email to {Email}", item.Email);
                    }
                }

                return ApiResponse<int>.SuccessRes(room.RoomId, "Room created and invitations sent successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Serilog.Log.Error(ex, "An error occurred while creating the room and processing invitations.");
                return ApiResponse<int>.Fail(0, "An error occurred while creating the room and processing invitations.");
            }
        }

        public async Task<ApiResponse> AddMemberAsync(AddMemberViewModel viewModel)
        {
            string currentUserId = _currentUser.UserId!;

            if (string.IsNullOrWhiteSpace(viewModel.Email))
            {
                return ApiResponse.Fail("Email is required.");
            }

            var room = await _context.Rooms
                .Include(r => r.Members)
                .FirstOrDefaultAsync(r => r.RoomId == viewModel.RoomId && !r.IsDeleted);

            if (room == null)
            {
                return ApiResponse.Fail("Room not found.");
            }

            if (!room.Members.Any(m => m.ApplicationUserId == currentUserId && !m.IsDeleted))
            {
                return ApiResponse.Fail("You are not authorized to add members to this room.");
            }

            var inviterUser = await _userManager.FindByIdAsync(currentUserId);
            var inviterName = inviterUser?.UserName ?? "A friend";

            var normalizedEmail = viewModel.Email.Trim().ToLowerInvariant();

            var existingMember = room.Members.FirstOrDefault(m =>
                !m.IsDeleted && m.Email != null && m.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (existingMember != null && !existingMember.IsPending)
            {
                return ApiResponse.Fail("This user is already an active member of the room.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            string token = Guid.NewGuid().ToString("N");
            string inviteeName = string.IsNullOrWhiteSpace(viewModel.Name) ? (existingMember?.Name ?? "Friend") : viewModel.Name.Trim();

            try
            {
                var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);

                if (existingMember == null)
                {
                    existingMember = new Member
                    {
                        RoomId = room.RoomId,
                        ApplicationUserId = existingUser?.Id,
                        Name = inviteeName,
                        Email = normalizedEmail,
                        IsPending = true,
                        JoinedDate = DateTimeProvider.NowIST,
                        CreatedDate = DateTimeProvider.NowIST,
                        CreatedByUserId = currentUserId
                    };
                    _context.Members.Add(existingMember);
                }
                else
                {
                    existingMember.Name = inviteeName;
                    if (existingUser != null && string.IsNullOrEmpty(existingMember.ApplicationUserId))
                    {
                        existingMember.ApplicationUserId = existingUser.Id;
                    }
                    existingMember.UpdatedDate = DateTimeProvider.NowIST;
                    existingMember.UpdatedByUserId = currentUserId;
                }

                // Expire any previous pending invitations for this room and email
                var existingInvitations = await _context.RoomInvitations
                    .Where(i => i.RoomId == room.RoomId && i.InvitedEmail == normalizedEmail && i.Status == InvitationStatus.Pending)
                    .ToListAsync();

                foreach (var oldInvite in existingInvitations)
                {
                    oldInvite.Status = InvitationStatus.Revoked;
                    oldInvite.UpdatedDate = DateTimeProvider.NowIST;
                    oldInvite.UpdatedByUserId = currentUserId;
                }

                var invitation = new RoomInvitation
                {
                    RoomId = room.RoomId,
                    InvitedEmail = normalizedEmail,
                    InvitedName = inviteeName,
                    Token = token,
                    Status = InvitationStatus.Pending,
                    ExpiresAt = DateTimeProvider.NowIST.AddDays(7),
                    CreatedDate = DateTimeProvider.NowIST,
                    CreatedByUserId = currentUserId
                };
                _context.RoomInvitations.Add(invitation);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                foreach (var member in room.Members.Where(m => !string.IsNullOrEmpty(m.ApplicationUserId)))
                {
                    _cacheService.ClearRoomsCache(member.ApplicationUserId!);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Serilog.Log.Error(ex, "Error while adding member / creating invitation for {Email}", viewModel.Email);
                return ApiResponse.Fail("An error occurred while adding the member.");
            }

            // Send invitation email
            var baseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "https://splitx-exp.netlify.app";
            var joinUrl = $"{baseUrl}/join-room?token={token}";

            try
            {
                var (subject, body) = await _emailTemplateService.GetRoomInvitationEmailAsync(inviterName, room.Name ?? "Room", joinUrl, inviteeName);
                await _emailSender.SendEmailAsync(normalizedEmail, subject, body);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to send invitation email to {Email}", normalizedEmail);
                return ApiResponse.SuccessRes("Member added, but could not send email notification. Please check email settings.");
            }

            return ApiResponse.SuccessRes("Invitation sent successfully! An email has been dispatched with a link to join the room.");
        }

        public async Task<ApiResponse> RemoveMemberAsync(int roomId, int memberId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var membership = await _context.Members.FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.MemberId == memberId && !rm.IsDeleted);

                if (membership == null)
                    return ApiResponse.Fail("Member not found in this room.");

                var hasFinancialRecords = await _context.Expenses.AnyAsync(e => (e.MemberId == memberId || e.ExpenseSplits.Any(s => s.MemberId == memberId)) && e.RoomId == roomId)
                    || await _context.Settlements.AnyAsync(s => (s.MemberId == memberId || s.PaidToMemberId == memberId) && s.RoomId == roomId);

                if (hasFinancialRecords)
                {
                    // Soft offboard: preserve full historical audit but mark as departed so they are excluded from future expenses
                    membership.LeftDate = DateTimeProvider.NowIST;
                }
                else
                {
                    // Member was added accidentally and has zero financial transactions
                    membership.IsDeleted = true;
                    membership.LeftDate = DateTimeProvider.NowIST;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var member = await _context.Members.FindAsync(memberId);
                if (member != null && !string.IsNullOrEmpty(member.ApplicationUserId))
                {
                    _cacheService.ClearRoomsCache(member.ApplicationUserId);
                }

                foreach (var memberData in _context.Members.Where(m => m.RoomId == roomId && !string.IsNullOrEmpty(m.ApplicationUserId)))
                {
                    _cacheService.ClearRoomsCache(memberData.ApplicationUserId!);
                }
                return ApiResponse.SuccessRes("Member removed successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return ApiResponse.Fail("An error occurred while removing the member.");
            }
        }

        public async Task<ApiResponse<InvitationDetailsResponse>> GetInvitationDetailsAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiResponse<InvitationDetailsResponse>.Fail(null!, "Invalid invitation token.");

            var invitation = await _context.RoomInvitations
                .Include(i => i.Room)
                    .ThenInclude(r => r.CreatedByUser)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
                return ApiResponse<InvitationDetailsResponse>.Fail(null!, "Invitation not found or invalid link.");

            bool isExpired = invitation.ExpiresAt < DateTimeProvider.NowIST;

            var inviter = !string.IsNullOrEmpty(invitation.CreatedByUserId)
                ? await _userManager.FindByIdAsync(invitation.CreatedByUserId)
                : invitation.Room.CreatedByUser;

            var existingUser = !string.IsNullOrWhiteSpace(invitation.InvitedEmail)
                ? await _userManager.FindByEmailAsync(invitation.InvitedEmail.Trim().ToLowerInvariant())
                : null;

            var response = new InvitationDetailsResponse
            {
                RoomId = invitation.RoomId,
                RoomName = invitation.Room.Name ?? "Unnamed Room",
                InviterName = inviter?.UserName ?? "A friend",
                InvitedEmail = invitation.InvitedEmail,
                InvitedName = invitation.InvitedName,
                Status = invitation.Status.ToString(),
                IsExpired = isExpired,
                ExpiresAt = invitation.ExpiresAt,
                IsRegisteredUser = existingUser != null
            };

            return ApiResponse<InvitationDetailsResponse>.SuccessRes(response, "Invitation details retrieved successfully.");
        }

        public async Task<ApiResponse> AcceptInvitationAsync(string token, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(token))
                return ApiResponse.Fail("Invalid invitation token.");

            var invitation = await _context.RoomInvitations
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.Token == token);

            if (invitation == null)
                return ApiResponse.Fail("Invitation not found or invalid link.");

            if (invitation.Status == InvitationStatus.Accepted)
            {
                return ApiResponse<int>.SuccessRes(invitation.RoomId, "You have already accepted this invitation.");
            }

            if (invitation.Status == InvitationStatus.Revoked)
            {
                return ApiResponse.Fail("This invitation has been revoked.");
            }

            if (invitation.ExpiresAt < DateTimeProvider.NowIST)
            {
                invitation.Status = InvitationStatus.Expired;
                invitation.UpdatedDate = DateTimeProvider.NowIST;
                invitation.UpdatedByUserId = currentUserId;
                await _context.SaveChangesAsync();
                return ApiResponse.Fail("This invitation link has expired. Please ask for a new invite.");
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
                return ApiResponse.Fail("User not found.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var normalizedEmail = invitation.InvitedEmail.Trim().ToLowerInvariant();

                // Find or create member in this room
                var member = await _context.Members
                    .FirstOrDefaultAsync(m => m.RoomId == invitation.RoomId && !m.IsDeleted &&
                        (m.ApplicationUserId == currentUserId ||
                         (m.Email != null && m.Email.ToLower() == normalizedEmail)));

                if (member != null)
                {
                    member.ApplicationUserId = currentUserId;
                    member.Email = currentUser.Email ?? normalizedEmail;
                    if (string.IsNullOrEmpty(member.Name) || member.Name == "Friend")
                    {
                        member.Name = currentUser.UserName ?? invitation.InvitedName;
                    }
                    member.IsPending = false;
                    member.JoinedDate = DateTimeProvider.NowIST;
                    member.UpdatedDate = DateTimeProvider.NowIST;
                    member.UpdatedByUserId = currentUserId;
                }
                else
                {
                    member = new Member
                    {
                        RoomId = invitation.RoomId,
                        ApplicationUserId = currentUserId,
                        Name = currentUser.UserName ?? invitation.InvitedName,
                        Email = currentUser.Email ?? normalizedEmail,
                        IsPending = false,
                        JoinedDate = DateTimeProvider.NowIST,
                        CreatedDate = DateTimeProvider.NowIST,
                        CreatedByUserId = currentUserId
                    };
                    _context.Members.Add(member);
                }

                invitation.Status = InvitationStatus.Accepted;
                invitation.AcceptedAt = DateTimeProvider.NowIST;
                invitation.UpdatedDate = DateTimeProvider.NowIST;
                invitation.UpdatedByUserId = currentUserId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _cacheService.ClearRoomsCache(currentUserId);

                return ApiResponse<int>.SuccessRes(invitation.RoomId, $"You have successfully joined '{invitation.Room.Name}'!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Serilog.Log.Error(ex, "Failed to accept invitation for token {Token} by user {UserId}", token, currentUserId);
                return ApiResponse.Fail("An error occurred while accepting the invitation.");
            }
        }

        public async Task ClaimPendingInvitationsAsync(string email, string userId)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userId)) return;

            var normalizedEmail = email.Trim().ToLowerInvariant();

            var pendingMembers = await _context.Members
                .Where(m => m.Email != null && m.Email.ToLower() == normalizedEmail && string.IsNullOrEmpty(m.ApplicationUserId))
                .ToListAsync();

            foreach (var member in pendingMembers)
            {
                member.ApplicationUserId = userId;
                member.UpdatedDate = DateTimeProvider.NowIST;
                member.UpdatedByUserId = userId;
            }

            if (pendingMembers.Any())
            {
                await _context.SaveChangesAsync();
                _cacheService.ClearRoomsCache(userId);
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

                foreach (var member in room.Members)
                {
                    member.IsDeleted = true;
                }
                room.IsDeleted = true;

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