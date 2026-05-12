using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class UserSecurityToken
    {
        public int Id { get; set; }

        public int UserId { get; set; }

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

        public User User { get; set; } = null!;
    }
}
