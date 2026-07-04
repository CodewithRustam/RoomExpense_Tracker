namespace Domain.Interfaces
{
    public interface IMemberRepository : IRepository<Member>
    {
        Task<IReadOnlyList<Member>> GetMembersByRoomId(int roomId, string? userId);

        Task<Member?> GetLoggedInMemberDetails(int roomId, string memberName, string? userId);

        Task<Member?> GetRecipientMemberDetails(int roomId, string paidToMemberName);

        Task<int> GetMemberIdAsync(string? userId, int roomId);

        Task<int> GetMemberCountAsync(int roomId);

        Task AddMembersAsync(IEnumerable<Member> members);

        Task<bool> MemberExistsAsync(int roomId, string username);

        Task<IReadOnlyList<string?>> GetDeviceTokensAsync(int roomId, string? userId);
    }
}