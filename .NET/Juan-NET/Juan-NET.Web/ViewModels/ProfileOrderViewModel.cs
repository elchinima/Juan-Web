namespace Juan_NET.Web.ViewModels
{
    public class ProfileOrderViewModel
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Currency { get; set; } = "usd";

        public decimal Subtotal { get; set; }

        public decimal DeliveryTotal { get; set; }

        public decimal DiscountTotal { get; set; }

        public decimal Total { get; set; }

        public string? PromoCode { get; set; }

        public bool CanRequestRefund { get; set; }

        public List<ProfileOrderItemViewModel> Items { get; set; } = new();
    }
}
