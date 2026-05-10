namespace Juan_NET.Web.ViewModels
{
    public class CategoryCardViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int ProductCount { get; set; }

        public bool IsFavorite { get; set; }
    }
}
