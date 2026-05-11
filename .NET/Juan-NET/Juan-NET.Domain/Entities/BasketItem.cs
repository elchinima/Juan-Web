namespace Juan_NET.Domain.Entities
{
    public class BasketItem
    {
        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        public int Quantity { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
