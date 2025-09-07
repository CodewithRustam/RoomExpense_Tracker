
using Domain.Entities;
using Services.ViewModels;
using Services.ViewModels.ApiReponse;

namespace Services.Interfaces
{
    public interface IRoomServices
    {
        Task<List<RoomResponse>> GetRoomsForCurrentUser();
        Task<bool> IsValidRoomAsync(int roomId);
        Task<RoomDetailsViewModel?> GetRoomDetails(int roomId, string? month, bool isFromSettled);
        Task<(bool success, string message)> CreateRoomAsync(RoomViewModel viewModel);
    }
}
