namespace Juan_NET.Domain.Entities
{
    public class FavoriteCategoryDigest
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        public DateTime SentForDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
