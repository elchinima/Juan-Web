namespace Juan_NET.Web.ViewModels
{
    public class CheckoutViewModel
    {
        public List<ShopListItemViewModel> Items { get; set; } = new();

        public string PublishableKey { get; set; } = string.Empty;

        public string Currency { get; set; } = "usd";

        public bool IsStripeConfigured { get; set; }

        public decimal Total => Items.Sum(item => item.Total);
    }
}
