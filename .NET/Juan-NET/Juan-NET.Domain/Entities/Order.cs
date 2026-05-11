using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class Order
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

        [MaxLength(120)]
        public string? StripeSessionId { get; set; }

        [MaxLength(80)]
        public string? PromoCode { get; set; }

        [MaxLength(12)]
        public string Currency { get; set; } = "usd";

        [MaxLength(40)]
        public string Status { get; set; } = "Paid";

        public decimal Subtotal { get; set; }

        public decimal DeliveryTotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
