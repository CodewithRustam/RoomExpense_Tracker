namespace Services.Management
{
    public class PasswordResetLinkService : IPasswordResetLinkService
    {
        private readonly IPasswordResetLinkRepository passwordResetLinkRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public PasswordResetLinkService(IPasswordResetLinkRepository _passwordResetLinkRepository, UserManager<ApplicationUser> userManager)
        {
            passwordResetLinkRepository = _passwordResetLinkRepository; 
            _userManager = userManager;
        }
        public async Task<string> AddPasswordResetLink(string Email)
        {
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null) return string.Empty;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var shortCode = Guid.NewGuid().ToString("N").Substring(0, 8);

            var resetLinkEntry = new PasswordResetLink
            {
                ShortCode = shortCode,
                Token = token,
                Email = Email,
                Expiry = DateTimeProvider.NowIST.AddHours(1)
            };
            await passwordResetLinkRepository.AddPasswordResetLink(resetLinkEntry);

            return shortCode;
        }

        public async Task<PasswordResetLink?> GetPasswordResetDetailsByShortCode(string code)
        {
            return await passwordResetLinkRepository.GetPasswordResetDetailsByShortCode(code);
        }

        public async Task DeletePasswordResetLink(PasswordResetLink passwordResetLink)
        {
            await passwordResetLinkRepository.DeletePasswordResetLink(passwordResetLink);
        }
    }
}
