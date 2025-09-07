using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class PasswordResetLinkRepository : Repository<PasswordResetLink>, IPasswordResetLinkRepository
    {
        private readonly AppDbContext _context;
        public PasswordResetLinkRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task AddPasswordResetLink(PasswordResetLink passwordResetLink)
        {
            try
            {
                await AddAsync(passwordResetLink);
                await SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<PasswordResetLink?> GetPasswordResetDetailsByShortCode(string code)
        {
            try
            {
                return await FirstOrDefaultAsync(x => x.ShortCode == code && x.Expiry > DateTime.Now);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
