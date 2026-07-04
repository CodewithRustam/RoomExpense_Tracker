namespace Infrastructure.Repositories
{
    public class MemberRepository : Repository<Member>, IMemberRepository
    {

        public MemberRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Fast aggregate call to fetch row counts without loading full entities.
        /// </summary>
        public async Task<int> GetMemberCountAsync(int roomId)
        {
            return await _context.Members.CountAsync(m => m.RoomId == roomId);
        }

        /// <summary>
        /// Highly optimized projection query. Uses .Select() to pull ONLY the ID integer 
        /// from the database instead of allocating a whole Member entity into memory.
        /// </summary>
        public async Task<int> GetMemberIdAsync(string? userId, int roomId)
        {
            if (string.IsNullOrEmpty(userId)) return 0;

            return await _context.Members
                .AsNoTracking()
                .Where(m => m.ApplicationUserId == userId && m.RoomId == roomId)
                .Select(m => m.MemberId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Returns a structured, unmodifiable collection with prioritized sorting.
        /// </summary>
        public async Task<IReadOnlyList<Member>> GetMembersByRoomId(int roomId, string? userId)
        {
            return await _context.Members
                .AsNoTracking()
                .Where(m => m.RoomId == roomId)
                .OrderByDescending(m => m.ApplicationUserId == userId)
                .ThenBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<Member?> GetRecipientMemberDetails(int roomId, string paidToMemberName)
        {
            return await FirstOrDefaultAsync(m => m.Name == paidToMemberName && m.RoomId == roomId);
        }

        public async Task<Member?> GetLoggedInMemberDetails(int roomId, string memberName, string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            return await FirstOrDefaultAsync(m => m.Name == memberName && m.RoomId == roomId && m.ApplicationUserId == userId);
        }

        /// <summary>
        /// SOLID Relocation: Adds batch members cleanly under the correct boundary context.
        /// </summary>
        public Task AddMembersAsync(IEnumerable<Member> members)
        {
            return _context.Members.AddRangeAsync(members);
        }

        /// <summary>
        /// SOLID Relocation: Scans member existences directly using the Member DbSet.
        /// </summary>
        public async Task<bool> MemberExistsAsync(int roomId, string username)
        {
            return await _context.Members
                .AnyAsync(m => m.RoomId == roomId && m.Name == username);
        }

        public async Task<IReadOnlyList<string?>> GetDeviceTokensAsync(int roomId, string? userId)
        {
            return await _context.Members
                   .AsNoTracking()
                   .Where(m => m.RoomId == roomId && m.ApplicationUserId != userId && m.ApplicationUser!.DeviceToken != null)
                   .Select(m => m.ApplicationUser!.DeviceToken)
                   .ToListAsync();
        }
    }
}