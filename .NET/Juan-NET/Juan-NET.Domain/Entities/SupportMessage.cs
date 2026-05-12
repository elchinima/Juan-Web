using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class SupportMessage
    {
        public int Id { get; set; }

        public int SupportTicketId { get; set; }

        public int SenderUserId { get; set; }

        public bool IsOperator { get; set; }

        [MaxLength(2000)]
        public string? Text { get; set; }

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public SupportTicket SupportTicket { get; set; } = null!;

        public User SenderUser { get; set; } = null!;
    }
}
