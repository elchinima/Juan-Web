using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Web.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
