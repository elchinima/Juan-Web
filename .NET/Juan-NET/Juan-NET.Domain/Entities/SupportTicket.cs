using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class SupportTicket
    {
        public int Id { get; set; }

        [Required, MaxLength(32)]
        public string Code { get; set; } = string.Empty;

        public int UserId { get; set; }

        public int? OperatorUserId { get; set; }

        [Required, MaxLength(160)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string Topic { get; set; } = "Other";

        [Required, MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        [Required, MaxLength(40)]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAt { get; set; }

        public User User { get; set; } = null!;

        public User? OperatorUser { get; set; }

        public SupportTicketCreatedDate? CreatedDate { get; set; }

        public SupportRating? Rating { get; set; }

        public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
    }
}
