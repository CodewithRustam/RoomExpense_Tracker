namespace Services.Interfaces
{
    public interface IRoomServices
    {
        Task<List<RoomResponse>> GetRoomsForCurrentUser();
        Task<bool> IsValidRoomAsync(int roomId);
        Task<RoomDetailsViewModel?> GetRoomDetails(int roomId, string? month, bool isFromSettled);
        Task<ApiResponse> CreateRoomAsync(RoomViewModel viewModel);
        Task<ApiResponse> AddMemberAsync(AddMemberViewModel viewModel);
        Task<ApiResponse> RemoveMemberAsync(int roomId, int memberId);
        Task<ApiResponse> DeleteRoomAsync(int roomId);
    }
}
