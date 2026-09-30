namespace Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateJwtToken(ApplicationUser user, string? deviceFingerprint = null);
        string EncryptTokenForClient(string token, string deviceFingerprint);
        bool ValidateDeviceFingerprint(string? expectedHash, string? providedFingerprint);
    }
}
