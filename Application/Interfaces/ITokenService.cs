namespace Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(ApplicationUser user);
    }
}
