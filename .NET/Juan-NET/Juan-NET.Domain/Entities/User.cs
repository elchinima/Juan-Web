using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(120), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(120)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(60)]
        public string PasswordSalt { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? ProfileImageUrl { get; set; }

        [MaxLength(60)]
        public string? ExternalProvider { get; set; }

        [MaxLength(120)]
        public string? ExternalProviderId { get; set; }

        public bool IsTwoFactorEnabled { get; set; }

        [MaxLength(120)]
        public string? TwoFactorCodeHash { get; set; }

        [MaxLength(60)]
        public string? TwoFactorCodeSalt { get; set; }

        public DateTime? TwoFactorCodeExpiresAt { get; set; }

        [MaxLength(120)]
        public string? PasswordResetTokenHash { get; set; }

        [MaxLength(60)]
        public string? PasswordResetTokenSalt { get; set; }

        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        [MaxLength(120)]
        public string? PendingPasswordHash { get; set; }

        [MaxLength(60)]
        public string? PendingPasswordSalt { get; set; }

        [MaxLength(120)]
        public string? PasswordChangeTokenHash { get; set; }

        [MaxLength(60)]
        public string? PasswordChangeTokenSalt { get; set; }

        public DateTime? PasswordChangeTokenExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
