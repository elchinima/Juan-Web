using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Web.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required, MaxLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }

        public bool IsTwoFactorEnabled { get; set; }

        public ChangePasswordViewModel ChangePassword { get; set; } = new();

        public List<User> Users { get; set; } = [];

        public IFormFile? ImageFile { get; set; }
    }
}
