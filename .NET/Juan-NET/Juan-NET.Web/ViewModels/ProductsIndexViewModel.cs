namespace Juan_NET.Web.ViewModels
{
    public class ProductsIndexViewModel
    {
        public List<Product> Products { get; set; } = new();

        public int CurrentPage { get; set; } = 1;

        public bool HasPreviousPage { get; set; }

        public bool HasNextPage { get; set; }

        public Dictionary<int, List<ProductReviewViewModel>> ReviewsByProductId { get; set; } = new();

        public Dictionary<int, ProductReviewSummaryViewModel> ReviewSummariesByProductId { get; set; } = new();

        public List<CategoryCardViewModel> Categories { get; set; } = new();

        public string? SearchTerm { get; set; }

        public int? CategoryId { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public decimal? MinRating { get; set; }
    }
}
