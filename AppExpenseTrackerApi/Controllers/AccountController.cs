using Domain.AppUser;
using ExpenseTrakcerHepler;
using Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;

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

        public AccountController(SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IPasswordResetLinkService passwordResetLinkService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _passwordResetLinkService = passwordResetLinkService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid request data."));

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName!, model.Password!, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return Ok(ApiResponse.Ok("Login successful"));

            return Unauthorized(ApiResponse.Fail("Invalid login attempt."));
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
                return Ok(ApiResponse.Ok("Registration successful"));
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<object>.Fail(null,string.Join(", ", errors)));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(ApiResponse.Ok("Logged out successfully"));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse.Fail("Invalid request data."));

            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user == null || string.IsNullOrEmpty(user.Email))
                return Ok(ApiResponse.Ok("Password reset link sent if email exists"));

            string shortCode = await _passwordResetLinkService.AddPasswordResetLink(model.Email!);
            var resetUrl = Url.Action("RedirectReset", "Account", new { code = shortCode }, Request.Scheme)!;

            var body = EmailTemplates.GetPasswordResetEmail(resetUrl);
            await _emailSender.SendEmailAsync(model.Email!, "Reset Your Password", body);

            return Ok(ApiResponse.Ok("Password reset link sent"));
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
                return Ok(ApiResponse.Ok("Password reset successful"));

            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(ApiResponse<object>.Fail(null,string.Join(", ", errors)));
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Ok(ApiResponse<object>.Ok(new { exists = false }, "Invalid email"));

            var user = await _userManager.FindByEmailAsync(email);
            return Ok(ApiResponse<object>.Ok(new { exists = user != null }, "Email check complete"));
        }
    }
}
