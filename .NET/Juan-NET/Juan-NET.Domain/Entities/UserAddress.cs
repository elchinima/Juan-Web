using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class UserAddress
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(80)]
        public string RecipientFullName { get; set; } = string.Empty;

        [Required, MaxLength(180)]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(180)]
        public string? AddressLine2 { get; set; }

        [Required, MaxLength(7)]
        public string Fin { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}
