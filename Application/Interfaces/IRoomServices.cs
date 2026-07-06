namespace Services.Interfaces
{
    public interface IRoomServices
    {
        Task<List<RoomResponse>> GetRoomsForCurrentUser();
        Task<bool> IsValidRoomAsync(int roomId);
        Task<RoomDetailsViewModel?> GetRoomDetails(int roomId, string? month, bool isFromSettled);
        Task<ApiResponse<int>> CreateRoomAsync(RoomViewModel viewModel, string currentUserId);
    }
}
