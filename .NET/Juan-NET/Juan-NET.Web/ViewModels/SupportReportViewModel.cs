namespace Juan_NET.Web.ViewModels
{
    public class SupportReportViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
