namespace Juan_NET.Web.Services
{
    public sealed class AdminAccessResult
    {
        public HashSet<string> Permissions { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public bool HasPermission(string permissionKey)
        {
            return permissionKey == AdminPermissionKeys.AdminAccess
                ? Permissions.Contains(AdminPermissionKeys.AdminAccess)
                : Permissions.Contains(AdminPermissionKeys.AdminAccess) && Permissions.Contains(permissionKey);
        }
    }
}
