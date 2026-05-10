using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class AdminRole
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Color { get; set; } = "#e3a51e";

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AdminRolePermission> Permissions { get; set; } = new List<AdminRolePermission>();

        public ICollection<UserAdminRole> UserRoles { get; set; } = new List<UserAdminRole>();
    }
}
