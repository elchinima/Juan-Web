namespace Juan_NET.Web.ViewModels
{
    public class SupportTicketDetailsViewModel
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Customer { get; set; } = string.Empty;

        public string Topic { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public decimal? Rating { get; set; }

        public string? RatingComment { get; set; }

        public IReadOnlyList<SupportMessageViewModel> Messages { get; set; } = [];
    }
}
