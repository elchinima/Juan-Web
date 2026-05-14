namespace Juan_NET.Infrastructure.Payments
{
    public class StripeSettings
    {
        public string PublishableKey { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public string Currency { get; set; } = "usd";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(PublishableKey) &&
            !string.IsNullOrWhiteSpace(SecretKey);
    }
}
