
namespace Juan_NET.Web.ViewModels
{
    public class TwoFactorViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required, StringLength(6, MinimumLength = 6)]
        public string Code { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
