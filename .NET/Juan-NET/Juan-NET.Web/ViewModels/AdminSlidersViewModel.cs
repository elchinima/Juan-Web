using Juan_NET.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Juan_NET.Web.ViewModels
{
    public class AdminSlidersViewModel
    {
        public List<Slider> Sliders { get; set; } = new();

        public Slider Slider { get; set; } = new();

        public IFormFile? ImageFile { get; set; }
    }
}
