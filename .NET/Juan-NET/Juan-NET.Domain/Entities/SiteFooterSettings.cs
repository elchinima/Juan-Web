using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class SiteFooterSettings
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string Address { get; set; } = "184 Main Rd E, St Albans VIC 3021, Australia";

        [Required, EmailAddress, MaxLength(180)]
        public string Email { get; set; } = "yourmail@gmail.com";

        [Required, MaxLength(40)]
        public string Phone { get; set; } = "+ 00 254 254565";

        [Required, MaxLength(250)]
        public string AllProductsUrl { get; set; } = "/Products";

        [Required, MaxLength(250)]
        public string CategoriesUrl { get; set; } = "/Categories";

        [Required, MaxLength(250)]
        public string HomeUrl { get; set; } = "/";

        [Required, MaxLength(250)]
        public string AboutUrl { get; set; } = "#";

        [Required, MaxLength(250)]
        public string ContactUrl { get; set; } = "/Home/Contact";

        [Required, MaxLength(250)]
        public string PrivacyUrl { get; set; } = "#";

        [Required, MaxLength(250)]
        public string FacebookUrl { get; set; } = "#";

        [Required, MaxLength(250)]
        public string TwitterUrl { get; set; } = "#";

        [Required, MaxLength(250)]
        public string LinkedinUrl { get; set; } = "#";

        [Required, MaxLength(250)]
        public string InstagramUrl { get; set; } = "#";
    }
}
