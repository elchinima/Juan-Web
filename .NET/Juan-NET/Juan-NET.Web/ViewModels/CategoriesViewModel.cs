namespace Juan_NET.Web.ViewModels
{
    public class CategoriesViewModel
    {
        public List<CategoryCardViewModel> Categories { get; set; } = new();

        public bool IsAuthenticated { get; set; }
    }
}
