using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPasswordResetLinkRepository
    {
        Task AddPasswordResetLink(PasswordResetLink passwordResetLink);
        Task<PasswordResetLink?> GetPasswordResetDetailsByShortCode(string code);
    }
}
