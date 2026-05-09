using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class Slider
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Subtitle { get; set; } = string.Empty;

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(300)]
        public string ImageUrl { get; set; } = "/main assets/img/slider/slider-1.jpg";

        [MaxLength(80)]
        public string ButtonText { get; set; } = "SHOP NOW";

        [MaxLength(300)]
        public string ButtonUrl { get; set; } = "/Products";

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
