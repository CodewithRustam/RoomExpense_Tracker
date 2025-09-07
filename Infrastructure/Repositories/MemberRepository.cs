using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                return await _context.Members.CountAsync(m => m.RoomId == roomId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> GetMemberId(string? userId, int roomId)
        {
            try
            {
                Member? member = await FirstOrDefaultAsync(m => m.ApplicationUserId == userId && m.RoomId == roomId);

                if (member != null)
                {
                    return member.MemberId;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return 0;
        }

        public async Task<List<Member>> GetMembersByRoomId(int roomId, string? userId)
        {
            try
            {
                return await _context.Members.Where(m => m.RoomId == roomId).OrderByDescending(m => m.ApplicationUserId == userId).ThenBy(m => m.Name).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Member?> GetRecipientMemberDetails(int roomId, string paidToMemberName)
        {
            try
            {
                return await FirstOrDefaultAsync(m => m.Name == paidToMemberName && m.RoomId == roomId);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<Member?> GetLoggedInMemberDetails(int roomId, string memberName, string? userId)
        {
            try
            {
                return await FirstOrDefaultAsync(m => m.Name == memberName && m.RoomId == roomId && m.ApplicationUserId == userId);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
