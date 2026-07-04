namespace Domain.Interfaces
{
    public interface IRoomRepository : IRepository<Room>
    {
        Task<IReadOnlyList<Room>> GetRoomsForCurrentUser(string? userId);

        Task<Room?> GetRoomDetails(int roomId, string? userId);

        Task<string?> GetRoomNameAsync(int roomId);

        Task<bool> IsValidRoomAsync(int roomId);
    }
}