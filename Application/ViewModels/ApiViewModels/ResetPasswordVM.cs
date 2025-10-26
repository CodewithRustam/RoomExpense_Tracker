namespace Services.ViewModels.ApiViewModels
{
    public class ResetPasswordVM
    {
        [Required]
        [DataType(DataType.Password)]
        [StringLength(20, ErrorMessage = "Password must be at least {2} characters.", MinimumLength = 6)]
        public string? Password { get; set; }
        public string? Token { get; set; }
    }
}
