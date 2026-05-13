namespace Juan_NET.Web.ViewModels
{
    public class SupportReportViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
