using Domain.AppUser;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiReponse;

namespace Services.Management
{
    public class RoomService : IRoomServices
    {
        private readonly IRoomRepository roomRepository;
        private readonly ICurrentUserService currentUser;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoomService(IRoomRepository _roomRepository, ICurrentUserService _currentUser, UserManager<ApplicationUser> userManager) 
        {
            roomRepository = _roomRepository;
            currentUser = _currentUser;
            _userManager = userManager;
        }

        public async Task<List<RoomResponse>> GetRoomsForCurrentUser()
        {
            try
            {
                string? userId = currentUser.UserId;
                var rooms = await roomRepository.GetRoomsForCurrentUser(userId);

                return rooms.Select(r => new RoomResponse
                {
                    RoomId = r.RoomId,
                    Name = r.Name!,
                    CreatedByUserId = r.CreatedByUserId,
                    CreatedDate = r.CreatedDate,
                    Members = r.Members.Select(m => new MemberResponse
                    {
                        MemberId = m.MemberId,
                        Name = m.Name
                    }).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                // Log the exception instead of rethrowing blindly
                throw new ApplicationException("Error while fetching rooms for the current user.", ex);
            }
        }


        public async Task<bool> IsValidRoomAsync(int roomId)
        {
			try
			{
                return await roomRepository.IsValidRoomAsync(roomId);
            }
            catch (Exception)
			{
				throw;
			}
        }
        public async Task<RoomDetailsViewModel?> GetRoomDetails(int roomId, string? month, bool isFromSettled)
        {
            try
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
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<(bool success, string message)> CreateRoomAsync(RoomViewModel viewModel)
        {
            try
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

                await roomRepository.AddRoomAsync(room);

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
            catch (Exception)
            {
                throw;
            }
        }
    }
}
