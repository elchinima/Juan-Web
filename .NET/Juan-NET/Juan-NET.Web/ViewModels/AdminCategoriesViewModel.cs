using Juan_NET.Domain.Entities;

namespace Juan_NET.Web.ViewModels
{
    public class AdminCategoriesViewModel
    {
        public List<Category> Categories { get; set; } = new();

        public Category Category { get; set; } = new();
    }
}
