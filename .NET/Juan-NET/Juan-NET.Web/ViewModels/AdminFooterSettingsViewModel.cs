using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Web.ViewModels
{
    public class AdminFooterSettingsViewModel
    {
        [Required, MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(180)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string Phone { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string AllProductsUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string CategoriesUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string HomeUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string AboutUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string ContactUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string PrivacyUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string FacebookUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string TwitterUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string LinkedinUrl { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string InstagramUrl { get; set; } = string.Empty;
    }
}
