using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Web.ViewModels
{
    public class AdminSubscribeViewModel
    {
        public List<User> Users { get; set; } = [];

        public List<Subscriber> Subscribers { get; set; } = [];

        public List<string> SelectedEmails { get; set; } = [];

        public bool SendToAll { get; set; }

        public string? UserSearch { get; set; }

        [Required, MaxLength(120)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
    }
}
