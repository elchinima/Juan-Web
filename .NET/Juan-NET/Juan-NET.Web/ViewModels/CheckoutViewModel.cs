namespace Juan_NET.Web.ViewModels
{
    public class CheckoutViewModel
    {
        public List<ShopListItemViewModel> Items { get; set; } = new();

        public string PublishableKey { get; set; } = string.Empty;

        public string Currency { get; set; } = "usd";

        public bool IsStripeConfigured { get; set; }

        public bool HasDeliveryInformation { get; set; }

        public string? PromoCode { get; set; }

        public decimal Total => Items.Sum(item => item.Total);

        public decimal DeliveryTotal => Items.Sum(item => Math.Round(item.Total * 0.10m, 2, MidpointRounding.AwayFromZero));

        public decimal PayableTotal => Total + DeliveryTotal;
    }
}
