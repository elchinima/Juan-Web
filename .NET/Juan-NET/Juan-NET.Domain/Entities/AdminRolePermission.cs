using System.ComponentModel.DataAnnotations;

namespace Juan_NET.Domain.Entities
{
    public class AdminRolePermission
    {
        public int AdminRoleId { get; set; }

        public AdminRole AdminRole { get; set; } = null!;

        [Required, MaxLength(80)]
        public string PermissionKey { get; set; } = string.Empty;
    }
}
