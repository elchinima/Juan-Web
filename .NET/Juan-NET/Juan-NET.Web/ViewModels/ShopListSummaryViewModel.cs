namespace Juan_NET.Web.ViewModels
{
    public class ShopListSummaryViewModel
    {
        public List<ShopListItemViewModel> BasketItems { get; set; } = new();

        public List<ShopListItemViewModel> WishlistItems { get; set; } = new();

        public decimal BasketTotal => BasketItems.Sum(item => item.Total);
    }
}
