namespace Services.ViewModels.ApiViewModels
{
    public class ResetPasswordVM
    {
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(20, ErrorMessage = "Password must be at least {2} characters.", MinimumLength = 6)]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Reset code is required.")]
        public string? Code { get; set; }
    }
}
