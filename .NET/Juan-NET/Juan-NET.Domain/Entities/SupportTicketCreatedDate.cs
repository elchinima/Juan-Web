namespace Juan_NET.Domain.Entities
{
    public class SupportTicketCreatedDate
    {
        public int Id { get; set; }

        public int SupportTicketId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public SupportTicket SupportTicket { get; set; } = null!;
    }
}
