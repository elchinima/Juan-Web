using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(120), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(120)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(60)]
        public string PasswordSalt { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? ProfileImageUrl { get; set; }

        [MaxLength(60)]
        public string? ExternalProvider { get; set; }

        [MaxLength(120)]
        public string? ExternalProviderId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();

        public ICollection<UserSecurityToken> SecurityTokens { get; set; } = new List<UserSecurityToken>();

        public ICollection<UserAdminRole> AdminRoles { get; set; } = new List<UserAdminRole>();

        public ICollection<UserFavoriteCategory> FavoriteCategories { get; set; } = new List<UserFavoriteCategory>();

        public ICollection<BasketItem> BasketItems { get; set; } = new List<BasketItem>();

        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

        public ICollection<SupportTicket> AssignedSupportTickets { get; set; } = new List<SupportTicket>();

        public ICollection<SupportMessage> SupportMessages { get; set; } = new List<SupportMessage>();

        public ICollection<SupportRating> SupportRatings { get; set; } = new List<SupportRating>();

        public ICollection<SupportRating> OperatorSupportRatings { get; set; } = new List<SupportRating>();

        public ICollection<SupportOperatorWorkTime> SupportOperatorWorkTimes { get; set; } = new List<SupportOperatorWorkTime>();
    }
}
