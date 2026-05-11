using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        [Required, MaxLength(120)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? ProductImageUrl { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal UnitDeliveryPrice { get; set; }

        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }

        public Order Order { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
