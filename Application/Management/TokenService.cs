using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Services.Management
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private const string AppSecret = "534547b7246ab0a42795d368685309340cf785c4169520b95263d2059120fe60";

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateJwtToken(ApplicationUser user, string? deviceFingerprint = null)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            // Bind token to the client's device fingerprint if provided
            if (!string.IsNullOrWhiteSpace(deviceFingerprint))
            {
                string dfpHash = ComputeFingerprintHash(deviceFingerprint);
                claims.Add(new Claim("dfp", dfpHash));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(30),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public bool ValidateDeviceFingerprint(string? expectedHash, string? providedFingerprint)
        {
            if (string.IsNullOrEmpty(expectedHash))
                return true; // Backward compatibility / unconstrained tokens

            if (string.IsNullOrWhiteSpace(providedFingerprint))
                return false;

            string computedHash = ComputeFingerprintHash(providedFingerprint);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHash),
                Encoding.UTF8.GetBytes(computedHash)
            );
        }

        public string EncryptTokenForClient(string token, string deviceFingerprint)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(deviceFingerprint))
                return token;

            byte[] passwordBytes = Encoding.UTF8.GetBytes(AppSecret + deviceFingerprint);
            byte[] saltBytes = Encoding.UTF8.GetBytes(deviceFingerprint);

            using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 100000, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(32);

            byte[] iv = new byte[12];
            RandomNumberGenerator.Fill(iv);

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(token);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[16];

            using var aes = new AesGcm(key, tagSizeInBytes: 16);
            aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

            // WebCrypto compatible format: [12 bytes IV] + [ciphertext] + [16 bytes tag]
            byte[] combined = new byte[iv.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, combined, iv.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, combined, iv.Length + ciphertext.Length, tag.Length);

            return Convert.ToBase64String(combined);
        }

        private string ComputeFingerprintHash(string deviceFingerprint)
        {
            var hmacKey = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "841720121eb4d009fd55e9bde86c27b3");
            using var hmac = new HMACSHA256(hmacKey);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(deviceFingerprint.Trim()));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
