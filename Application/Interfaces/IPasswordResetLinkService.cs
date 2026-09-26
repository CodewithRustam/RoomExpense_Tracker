namespace Services.Interfaces
{
    public interface IPasswordResetLinkService
    {
        Task<string> AddPasswordResetLink(string Email);
        Task<PasswordResetLink?> GetPasswordResetDetailsByShortCode(string code);
        Task DeletePasswordResetLink(PasswordResetLink passwordResetLink);
    }
}
