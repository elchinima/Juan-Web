namespace Juan_NET.Web.ViewModels
{
    public class SupportMessageViewModel
    {
        public string SenderName { get; set; } = string.Empty;

        public bool IsOperator { get; set; }

        public string? Text { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
