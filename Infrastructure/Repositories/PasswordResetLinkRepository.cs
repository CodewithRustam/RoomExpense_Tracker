using ExpenseTrakcerHepler;

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
            await AddAsync(passwordResetLink);
            await _context.SaveChangesAsync();
        }

        public async Task<PasswordResetLink?> GetPasswordResetDetailsByShortCode(string code)
        {
            return await FirstOrDefaultAsync(x => x.ShortCode == code);
        }

        public async Task DeletePasswordResetLink(PasswordResetLink passwordResetLink)
        {
            Delete(passwordResetLink);
            await _context.SaveChangesAsync();
        }
    }
}
