namespace Juan_NET.Web.ViewModels
{
    public class AdminRefundOrderViewModel
    {
        public int Id { get; set; }

        public string Customer { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Currency { get; set; } = "usd";

        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RefundRequestedAt { get; set; }

        public DateTime? RefundedAt { get; set; }

        public string ItemsSummary { get; set; } = string.Empty;
    }
}
