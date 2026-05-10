namespace Juan_NET.Domain.Entities
{
    public class UserFavoriteCategory
    {
        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
