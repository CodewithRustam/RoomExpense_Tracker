using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IMemberRepository : IRepository<Member>
    {
        Task<Member?> GetLoggedInMemberDetails(int roomId, string memberName, string? userId);
        Task<int> GetMemberId(string? userId, int roomId);
        Task<List<Member>> GetMembersByRoomId(int roomId, string? userId);
        Task<Member?> GetRecipientMemberDetails(int roomId, string paidToMemberName);
        Task<int> GetMemberCount(int roomId);
    }
}
