using System.ComponentModel.DataAnnotations;
using Juan_NET.Web.Services;

namespace Juan_NET.Web.ViewModels
{
    public class AdminRolesViewModel
    {
        public List<AdminRole> Roles { get; set; } = [];

        public List<User> Users { get; set; } = [];

        public List<AdminPermissionItem> AvailablePermissions { get; set; } = [];

        public AdminRoleFormViewModel Role { get; set; } = new();

        public List<string> SelectedPermissionKeys { get; set; } = [];

        public List<int> SelectedUserIds { get; set; } = [];

        public int? EditingRoleId { get; set; }

        public int CurrentUserHighestRoleOrder { get; set; } = int.MaxValue;

        public int AssignRoleId { get; set; }
    }

    public class AdminRoleFormViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Color { get; set; } = "#e3a51e";
    }
}
