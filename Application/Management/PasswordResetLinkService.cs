using Domain.AppUser;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Services.Interfaces;

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
			try
			{
                var user = await _userManager.FindByEmailAsync(Email);
                var token = await _userManager.GeneratePasswordResetTokenAsync(user!);

                var shortCode = Guid.NewGuid().ToString("N").Substring(0, 8);

                var resetLinkEntry = new PasswordResetLink
                {
                    ShortCode = shortCode,
                    Token = token,
                    Email = Email,
                    Expiry = DateTime.Now.AddHours(1)
                };
                await passwordResetLinkRepository.AddPasswordResetLink(resetLinkEntry);

                return shortCode;
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
                return await passwordResetLinkRepository.GetPasswordResetDetailsByShortCode(code);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
