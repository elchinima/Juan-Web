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

        [Required, MaxLength(80)]
        public string DeliveryRecipientFullName { get; set; } = string.Empty;

        [Required, MaxLength(180)]
        public string DeliveryAddressLine1 { get; set; } = string.Empty;

        [MaxLength(180)]
        public string? DeliveryAddressLine2 { get; set; }

        [Required, RegularExpression(@"^[A-Za-z0-9]{7}$", ErrorMessage = "FIN must contain exactly 7 letters or digits.")]
        public string DeliveryFin { get; set; } = string.Empty;

        public bool HasDeliveryInformation { get; set; }

        public ChangePasswordViewModel ChangePassword { get; set; } = new();

        public List<ProfileOrderViewModel> Orders { get; set; } = new();

        public List<User> Users { get; set; } = [];

        public IFormFile? ImageFile { get; set; }
    }
}
