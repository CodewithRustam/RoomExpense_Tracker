using AppExpenseTracker.ViewModels;
using Domain.AppUser;
using Domain.Entities;
using ExpenseTrakcerHepler;
using Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Services.Management;
using Services.ViewModels;

namespace AppExpenseTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IPasswordResetLinkService _passwordResetLinkService;

        public AccountController(SignInManager<ApplicationUser> signInManager,UserManager<ApplicationUser> userManager, IEmailSender emailSender, IPasswordResetLinkService passwordResetLinkService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _passwordResetLinkService = passwordResetLinkService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName!, model.Password!, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Rooms");

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        public IActionResult AccessDenied() => View();

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email!);

            if (user == null || string.IsNullOrEmpty(user.Email))
                return RedirectToAction(nameof(ForgotPasswordConfirmation));

            string shortCode = await _passwordResetLinkService.AddPasswordResetLink(model.Email!);
           
            var shortUrl = Url.Action("RedirectReset", "Account", new { code = shortCode }, Request.Scheme)!;

            var body = EmailTemplates.GetPasswordResetEmail(shortUrl);
            await _emailSender.SendEmailAsync(model.Email!, "Reset Your Password", body);

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        public IActionResult ForgotPasswordConfirmation() => View();
        public async Task<IActionResult> RedirectReset(string code)
        {
            var entry = await _passwordResetLinkService.GetPasswordResetDetailsByShortCode(code);

            if (entry == null)
            {
                return BadRequest("Invalid or expired password reset link.");
            }

            // Now redirect to real reset page with actual token + email
            return RedirectToAction("ResetPassword", new { token = entry.Token, email = entry.Email });
        }


        [HttpGet]
        public IActionResult ResetPassword(string token, string email) =>
            View(new ResetPasswordViewModel { Token = token, Email = email });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user == null)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            var result = await _userManager.ResetPasswordAsync(user, model.Token!, model.Password!);

            if (result.Succeeded)
                return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        public IActionResult ResetPasswordConfirmation() => View();

        [HttpPost]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Json(new { exists = false });

            var user = await _userManager.FindByEmailAsync(email);
            return Json(new { exists = user != null });
        }

    }
}
