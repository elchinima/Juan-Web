using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(180)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(40)]
        public string Status { get; set; } = "New";

        [MaxLength(180)]
        public string? StatusChangedByEmail { get; set; }

        public DateTime? StatusChangedAt { get; set; }

        [MaxLength(100)]
        public string? AdminNote { get; set; }
    }
}
