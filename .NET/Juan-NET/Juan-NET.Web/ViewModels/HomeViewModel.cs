using Juan_NET.Domain.Entities;

namespace Juan_NET.Web.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> Products { get; set; } = new();

        public List<Slider> Sliders { get; set; } = new();
    }
}
