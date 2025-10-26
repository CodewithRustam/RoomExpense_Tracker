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
            await SaveChangesAsync();
        }

        public async Task<PasswordResetLink?> GetPasswordResetDetailsByShortCode(string code)
        {
            return await FirstOrDefaultAsync(x => x.ShortCode == code && x.Expiry >= DateTimeProvider.NowIST);
        }
    }
}
