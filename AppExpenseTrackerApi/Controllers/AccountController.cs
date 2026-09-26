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

            var user = await _userManager.FindByNameAsync(model.UserName!);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password!))
                return Unauthorized(ApiResponse.Fail("Invalid login attempt."));

            var token = _tokenService.GenerateJwtToken(user);

            // Custom response object to accommodate the token
            return Ok(new { success = true, message = "Login successful", token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<List<string>>.Fail(GetModelStateErrors(), "Validation failed."));

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
                string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
                string ipRateLimitKey = $"RateLimit_ForgotPassword_IP_{clientIp}";

                if (_rateLimitService.IsRateLimited(ipRateLimitKey, 5, TimeSpan.FromMinutes(1)))
                {
                    return BadRequest(ApiResponse.Fail("Too many requests from your connection. Please wait 1 minute."));
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
                    return BadRequest(ApiResponse.Fail("Please wait 1 minute before requesting another password reset email."));
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

        #endregion
    }
}