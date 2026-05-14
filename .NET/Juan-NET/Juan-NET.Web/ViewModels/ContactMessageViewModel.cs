
namespace Juan_NET.Web.ViewModels
{
    public class ContactMessageViewModel
    {
        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(180)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
    }
}
