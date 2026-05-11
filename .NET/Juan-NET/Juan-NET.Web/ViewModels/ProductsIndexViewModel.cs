namespace Juan_NET.Web.ViewModels
{
    public class ProductsIndexViewModel
    {
        public List<Product> Products { get; set; } = new();

        public int CurrentPage { get; set; } = 1;

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }
    }
}
