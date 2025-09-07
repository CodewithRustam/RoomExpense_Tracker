using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RoomRepository : Repository<Room>, IRoomRepository
    {
        private readonly AppDbContext _context;
        public RoomRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<Room>> GetRoomsForCurrentUser(string? userId)
        {
            try
            {
                return await _context.Rooms.Where(r => r.Members.Any(m => m.ApplicationUserId == userId) && !r.IsDeleted)
                                           .Include(r => r.Members).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<Room?> GetRoomDetails(int roomId, string? userId)
        {
            try
            {
                return await _context.Rooms.Include(r => r.Members).Include(r => r.Expenses).ThenInclude(e => e.Member)
                                      .FirstOrDefaultAsync(r => r.RoomId == roomId && r.Members.Any(m => m.ApplicationUserId == userId));
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<bool> IsValidRoomAsync(int roomId)
        {
            try
            {
                return await AnyAsync(r => r.RoomId == roomId);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task AddRoomAsync(Room room)
        {
            await AddAsync(room);
            await SaveChangesAsync();
        }

        public async Task AddMembersAsync(IEnumerable<Member> members)
        {
            await _context.Members.AddRangeAsync(members);
            await SaveChangesAsync();
        }

        public async Task<bool> MemberExistsAsync(int roomId, string userName)
        {
            return await _context.Members.AnyAsync(m => m.RoomId == roomId && m.Name == userName);
        }

        public  string? GetRoomName(int roomId)
        {
            return _context.Rooms.Where(x=>x.RoomId == roomId).Select(x=>x.Name).FirstOrDefault();
        }
    }
}
