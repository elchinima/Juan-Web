
namespace Juan_NET.Web.ViewModels
{
    public class SupportMessageInput
    {
        public int? TicketId { get; set; }

        [MaxLength(40)]
        public string? Topic { get; set; }

        [MaxLength(160)]
        public string? IssueTitle { get; set; }

        [MaxLength(2000)]
        public string? Text { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
