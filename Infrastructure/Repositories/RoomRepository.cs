namespace Infrastructure.Repositories
{
    public class RoomRepository : Repository<Room>, IRoomRepository
    {
        public RoomRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves all non-deleted rooms for a specific user with members and expenses eagerly loaded.
        /// </summary>
        public async Task<IReadOnlyList<Room>> GetRoomsForCurrentUser(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return Array.Empty<Room>();

            return await _context.Rooms
                .AsNoTracking()
                .Where(r => !r.IsDeleted && r.Members.Any(m => m.ApplicationUserId == userId && !m.IsDeleted))
                .Include(r => r.Members)
                .Include(r => r.Expenses)
                .ToListAsync();
        }

        /// <summary>
        /// Gets room details including member data and related expense mappings.
        /// </summary>
        public async Task<Room?> GetRoomDetails(int roomId, string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            return await _context.Rooms
                .AsNoTracking()
                .Include(r => r.Members)
                .Include(r => r.Expenses)
                    .ThenInclude(e => e.Member)
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.Members.Any(m => m.ApplicationUserId == userId));
        }

        /// <summary>
        /// Asynchronously fetches only the room's name projection.
        /// </summary>
        public async Task<string?> GetRoomNameAsync(int roomId)
        {
            return await _context.Rooms
                .Where(x => x.RoomId == roomId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Leverages the base repository's AnyAsync logic to validate room existence.
        /// </summary>
        public Task<bool> IsValidRoomAsync(int roomId)
        {
            return AnyAsync(x => x.RoomId == roomId);
        }
    }
}