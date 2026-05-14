namespace Juan_NET.Web.ViewModels
{
    public class ProductReviewViewModel
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string? UserImageUrl { get; set; }

        public decimal Rating { get; set; }

        public string? Comment { get; set; }

        public bool IsVerifiedPurchase { get; set; }

        public bool CanDelete { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
