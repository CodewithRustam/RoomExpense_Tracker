using System.ComponentModel.DataAnnotations;

namespace AppExpenseTracker.ViewModels
{
    public class RegisterViewModel
    {

        [Required]
        public string UserName { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
