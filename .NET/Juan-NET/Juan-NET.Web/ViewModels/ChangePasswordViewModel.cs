
namespace Juan_NET.Web.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(60), DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
