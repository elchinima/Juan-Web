
namespace Juan_NET.Web.ViewModels
{
    public class AdminCategoriesViewModel
    {
        public List<Category> Categories { get; set; } = new();

        public string? Search { get; set; }

        public Category Category { get; set; } = new();
    }
}
