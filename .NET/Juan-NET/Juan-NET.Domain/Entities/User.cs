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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
