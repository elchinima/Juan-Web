using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(240)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Range(0, 100000)]
        public int StockCount { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        [MaxLength(600)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

        public ICollection<BasketItem> BasketItems { get; set; } = new List<BasketItem>();

        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    }
}
