namespace Juan_NET.Web.ViewModels
{
    public class ProfileOrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal UnitDeliveryPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
