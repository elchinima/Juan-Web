
namespace Juan_NET.Web.ViewModels
{
    public class RegisterViewModel
    {
        [Required, MaxLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(120), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(60), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password)), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
