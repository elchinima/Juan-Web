using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Web.ViewModels
{
    public class ProductReviewInput
    {
        public int ProductId { get; set; }

        [Range(1, 5)]
        public decimal Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
