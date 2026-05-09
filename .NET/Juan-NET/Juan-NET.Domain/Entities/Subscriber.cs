namespace Juan_NET.Domain.Entities
{
    public class Subscriber
    {
        public int Id { get; set; }

        [Required, MaxLength(120), EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
