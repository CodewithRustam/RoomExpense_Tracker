namespace Infrastructure.Repositories
{
    public class MemberRepository : Repository<Member>, IMemberRepository
    {
        private readonly AppDbContext _context;

        public MemberRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<int> GetMemberCount(int roomId)
        {
            return await _context.Members.CountAsync(m => m.RoomId == roomId);
        }

        public async Task<int> GetMemberId(string? userId, int roomId)
        {
            Member? member = await FirstOrDefaultAsync(m => m.ApplicationUserId == userId && m.RoomId == roomId);

            if (member != null)
            {
                return member.MemberId;
            }
            return 0;
        }

        public async Task<List<Member>> GetMembersByRoomId(int roomId, string? userId)
        {
            return await _context.Members.Where(m => m.RoomId == roomId).OrderByDescending(m => m.ApplicationUserId == userId).ThenBy(m => m.Name).ToListAsync();
        }
        public async Task<Member?> GetRecipientMemberDetails(int roomId, string paidToMemberName)
        {
            return await FirstOrDefaultAsync(m => m.Name == paidToMemberName && m.RoomId == roomId);
        }
        public async Task<Member?> GetLoggedInMemberDetails(int roomId, string memberName, string? userId)
        {
            return await FirstOrDefaultAsync(m => m.Name == memberName && m.RoomId == roomId && m.ApplicationUserId == userId);
        }
    }
}
