using Juan_NET.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Juan_NET.Web.ViewModels
{
    public class AdminProductsViewModel
    {
        public List<Product> Products { get; set; } = new();

        public List<Category> Categories { get; set; } = new();

        public string? Search { get; set; }

        public List<int> SelectedCategoryIds { get; set; } = new();

        public IFormFile? ImageFile { get; set; }

        public Product Product { get; set; } = new();
    }
}
