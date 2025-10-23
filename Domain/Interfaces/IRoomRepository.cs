using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IRoomRepository : IRepository<Room>
    {
        Task<List<Room>> GetRoomsForCurrentUser(string? userId);
        Task<Room?> GetRoomDetails(int roomId, string? userId);
        Task<bool> MemberExistsAsync(int roomId, string username);
        Task AddMembersAsync(IEnumerable<Member> members);
        string? GetRoomName(int roomId);
        Task<bool> IsValidRoomAsync(int roomId);
    }
}
