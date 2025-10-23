using Domain.AppUser;
using ExpenseTrakcerHepler;
using Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,IPasswordResetLinkService passwordResetLinkService,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _passwordResetLinkService = passwordResetLinkService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid request data."));

            var user = await _userManager.FindByNameAsync(model.UserName!);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password!))
                return Unauthorized(ApiResponse.Fail("Invalid login attempt."));

            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddDays(30),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new { success = true, message = "Login successful", token = tokenHandler.WriteToken(token) });
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid request data."));

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Ok(ApiResponse.SuccessRes("Registration successful"));
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<object>.Fail(null,string.Join(", ", errors)));
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
                return BadRequest(ApiResponse.Fail("Invalid request data."));

            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user == null || string.IsNullOrEmpty(user.Email))
                return Ok(ApiResponse.SuccessRes("Password reset link sent if email exists"));

            string shortCode = await _passwordResetLinkService.AddPasswordResetLink(model.Email!);
            var resetUrl = Url.Action("RedirectReset", "Account", new { code = shortCode }, Request.Scheme)!;

            var body = EmailTemplates.GetPasswordResetEmail(resetUrl);
            await _emailSender.SendEmailAsync(model.Email!, "Reset Your Password", body);

            return Ok(ApiResponse.SuccessRes("Password reset link sent"));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid request data."));

            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user == null)
                return BadRequest(ApiResponse.Fail("Invalid user."));

            var result = await _userManager.ResetPasswordAsync(user, model.Token!, model.Password!);

            if (result.Succeeded)
                return Ok(ApiResponse.SuccessRes("Password reset successful"));

            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<object>.Fail(null,string.Join(", ", errors)));
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Ok(ApiResponse<object>.Fail(new { exists = false }, "Invalid email"));

            var user = await _userManager.FindByEmailAsync(email);
            return Ok(ApiResponse<object>.SuccessRes(new { exists = user != null }, "Email check complete"));
        }

        [HttpPost("app-register")]
        public async Task<IActionResult> RegisterDevice([FromBody] DeviceTokenModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId!);
            if (user == null) return NotFound();

            user.DeviceToken = model.DeviceToken;
            await _userManager.UpdateAsync(user);

            return Ok();
        }
    }
    public class DeviceTokenModel
    {
        public string? UserId { get; set; }
        public string? DeviceToken { get; set; }
    }
}
