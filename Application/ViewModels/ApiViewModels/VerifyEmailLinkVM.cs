namespace Services.ViewModels.ApiViewModels
{
    public class VerifyEmailLinkVM
    {
        [Required(ErrorMessage = "Reset code is required.")]
        public string? Code { get; set; }
    }
}
