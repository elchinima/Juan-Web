using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class ProductReview
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int UserId { get; set; }

        [Range(1, 5)]
        public decimal Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public bool IsVerifiedPurchase { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Product Product { get; set; } = null!;

        public User User { get; set; } = null!;
    }
}
