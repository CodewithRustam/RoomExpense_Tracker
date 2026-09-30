namespace AppExpenseTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IPasswordResetLinkService _passwordResetLinkService;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly IRateLimitService _rateLimitService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IPasswordResetLinkService passwordResetLinkService,
            IConfiguration configuration,
            ITokenService tokenService,
            IRateLimitService rateLimitService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _passwordResetLinkService = passwordResetLinkService;
            _configuration = configuration;
            _tokenService = tokenService;
            _rateLimitService = rateLimitService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid request data."));

            string clientIp = GetClientIpAddress();
            string ipRateLimitKey = $"RateLimit_Login_IP_{clientIp}";

            // IP Rate Limit: 10 attempts per minute
            if (_rateLimitService.IsRateLimited(ipRateLimitKey, 10, TimeSpan.FromMinutes(1)))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse.Fail("Too many login attempts from your connection. Please wait 1 minute before trying again."));
            }

            // User-based Rate Limit: 5 failed attempts per minute
            var identifier = model.UserName?.Trim().ToLowerInvariant() ?? string.Empty;
            string userRateLimitKey = $"RateLimit_Login_User_{identifier}";

            if (!string.IsNullOrEmpty(identifier) && _rateLimitService.IsRateLimited(userRateLimitKey, 5, TimeSpan.FromMinutes(1)))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse.Fail("Too many failed attempts for this account. Please wait 1 minute before trying again."));
            }

            var user = await _userManager.FindByNameAsync(model.UserName!) ?? await _userManager.FindByEmailAsync(model.UserName!);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password!))
                return Unauthorized(ApiResponse.Fail("Invalid login attempt."));

            // On successful login, clear the user rate limit counter
            if (!string.IsNullOrEmpty(identifier))
            {
                _rateLimitService.Reset(userRateLimitKey);
            }

            string? deviceFingerprint = Request.Headers["X-Device-Fingerprint"].FirstOrDefault();

            var token = _tokenService.GenerateJwtToken(user, deviceFingerprint);

            if (!string.IsNullOrWhiteSpace(deviceFingerprint))
            {
                // Encrypt token payload with client-derived key so raw JWT is never exposed in response body
                string encryptedToken = _tokenService.EncryptTokenForClient(token, deviceFingerprint);
                return Ok(new { success = true, message = "Login successful", token = encryptedToken, encrypted = true });
            }

            // Fallback for Swagger or direct tools without device fingerprint
            return Ok(new { success = true, message = "Login successful", token, encrypted = false });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<List<string>>.Fail(GetModelStateErrors(), "Validation failed."));

            string clientIp = GetClientIpAddress();
            string ipRateLimitKey = $"RateLimit_Register_IP_{clientIp}";

            // IP Rate Limit: Max 3 registration attempts per 5 minutes per IP
            if (_rateLimitService.IsRateLimited(ipRateLimitKey, 3, TimeSpan.FromMinutes(5)))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse.Fail("Too many registration attempts from this connection. Please try again in 5 minutes."));
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email!);
            if (existingUser != null)
                return Conflict(ApiResponse.Fail("Email is already registered."));

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (result.Succeeded)
            {
                // Note: If you are strictly using JWTs, SignInManager is cookie-based and may be unnecessary here.
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Ok(ApiResponse.SuccessRes("Registration successful."));
            }

            var identityErrors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<List<string>>.Fail(identityErrors, "User registration failed."));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(ApiResponse.SuccessRes("Logged out successfully"));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<List<string>>.Fail(GetModelStateErrors(), "Validation failed."));

            try
            {
                // 1. IP-based Rate Limit: Protect server & database against spam/DDoS from fake emails
                string clientIp = GetClientIpAddress();
                string ipRateLimitKey = $"RateLimit_ForgotPassword_IP_{clientIp}";

                if (_rateLimitService.IsRateLimited(ipRateLimitKey, 5, TimeSpan.FromMinutes(1)))
                {
                    return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse.Fail("Too many requests from your connection. Please wait 1 minute."));
                }

                // 2. Database User Lookup: Verify email existence
                var emailNormalized = model.Email!.Trim().ToLowerInvariant();
                var user = await _userManager.FindByEmailAsync(emailNormalized);

                if (user == null || string.IsNullOrEmpty(user.Email))
                    return BadRequest(ApiResponse.Fail("User with this email address does not exist."));

                // 3. Per-Email Rate Limit: Protect user's inbox from multiple emails
                string emailRateLimitKey = $"RateLimit_ForgotPassword_Email_{emailNormalized}";

                if (_rateLimitService.IsRateLimited(emailRateLimitKey, 1, TimeSpan.FromMinutes(1)))
                {
                    return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse.Fail("Please wait 1 minute before requesting another password reset email."));
                }

                string shortCode = await _passwordResetLinkService.AddPasswordResetLink(user.Email);

                if (string.IsNullOrEmpty(shortCode))
                    return BadRequest(ApiResponse.Fail("Could not generate password reset link."));

                var baseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "https://splitx-exp.netlify.app";
                string resetUrl = $"{baseUrl}/reset/reset-password?code={shortCode}";

                string memberName = string.IsNullOrEmpty(user.UserName) ? "User" : user.UserName;
                string body = EmailTemplates.GetPasswordResetEmail(resetUrl, memberName);

                await _emailSender.SendEmailAsync(user.Email, "Reset Your Password", body);

                return Ok(ApiResponse.SuccessRes("Password reset link sent successfully."));
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error in ForgotPassword for email {Email}", model?.Email);
                return BadRequest(ApiResponse.Fail("Failed to process password reset request. Please try again later."));
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = GetModelStateErrors();
                return BadRequest(ApiResponse<List<string>>.Fail(validationErrors, string.Join(", ", validationErrors)));
            }

            string clientIp = GetClientIpAddress();
            string ipRateLimitKey = $"RateLimit_ResetPassword_IP_{clientIp}";

            // IP Rate Limit: Max 5 reset attempts per 5 minutes per IP
            if (_rateLimitService.IsRateLimited(ipRateLimitKey, 5, TimeSpan.FromMinutes(5)))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse.Fail("Too many password reset attempts from this connection. Please wait 5 minutes before trying again."));
            }

            var resetLink = await _passwordResetLinkService.GetPasswordResetDetailsByShortCode(model.Code!);

            if (resetLink == null)
                return BadRequest(ApiResponse.Fail("Invalid password reset link."));

            if (resetLink.Expiry < DateTimeProvider.NowIST)
                return BadRequest(ApiResponse.Fail("Password link is Expired."));

            var user = await _userManager.FindByEmailAsync(resetLink.Email!);
            if (user == null)
                return BadRequest(ApiResponse.Fail("Invalid user."));

            var result = await _userManager.ResetPasswordAsync(user, resetLink.Token!, model.Password!);

            if (result.Succeeded)
            {
                await _passwordResetLinkService.DeletePasswordResetLink(resetLink);
                return Ok(ApiResponse.SuccessRes("Password reset successful"));
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<List<string>>.Fail(errors, string.Join(", ", errors)));
        }

        [HttpPost("verify-resetpassword-link")]
        public async Task<IActionResult> VerifyResetPasswordLink([FromBody] VerifyEmailLinkVM verifyEmailLink)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid request data."));

            string clientIp = GetClientIpAddress();
            string ipRateLimitKey = $"RateLimit_VerifyResetLink_IP_{clientIp}";

            // IP Rate Limit: Max 10 verification attempts per 1 minute
            if (_rateLimitService.IsRateLimited(ipRateLimitKey, 10, TimeSpan.FromMinutes(1)))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse.Fail("Too many verification attempts from this connection. Please wait 1 minute."));
            }

            var resetLink = await _passwordResetLinkService.GetPasswordResetDetailsByShortCode(verifyEmailLink.Code!);

            if (resetLink == null)
                return BadRequest(ApiResponse.Fail("Invalid password reset link."));

            if (resetLink.Expiry < DateTimeProvider.NowIST)
                return BadRequest(ApiResponse.Fail("Password link is Expired."));

            return Ok(ApiResponse.SuccessRes("Token is valid"));
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(ApiResponse<object>.Fail(new { exists = false }, "Invalid email"));

            var user = await _userManager.FindByEmailAsync(email);
            return Ok(ApiResponse<object>.SuccessRes(new { exists = user != null }, "Email check complete"));
        }

        #region Helpers

        private List<string> GetModelStateErrors()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
        }

        private string GetClientIpAddress()
        {
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ip)) return ip;
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        }

        #endregion
    }
}