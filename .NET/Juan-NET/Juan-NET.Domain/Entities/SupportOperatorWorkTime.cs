namespace Juan_NET.Domain.Entities
{
    public class SupportOperatorWorkTime
    {
        public int Id { get; set; }

        public int OperatorUserId { get; set; }

        public DateTime WorkDate { get; set; }

        public int TotalSeconds { get; set; }

        public DateTime? LastStartedAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User OperatorUser { get; set; } = null!;
    }
}
