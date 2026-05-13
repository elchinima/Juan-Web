using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class SupportRating
    {
        public int Id { get; set; }

        public int SupportTicketId { get; set; }

        public int UserId { get; set; }

        public int OperatorUserId { get; set; }

        public decimal Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public SupportTicket SupportTicket { get; set; } = null!;

        public User User { get; set; } = null!;

        public User OperatorUser { get; set; } = null!;
    }
}
