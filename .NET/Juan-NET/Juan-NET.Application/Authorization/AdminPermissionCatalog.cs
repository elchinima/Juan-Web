namespace Juan_NET.Application.Authorization
{
    public static class AdminPermissionCatalog
    {
        public static IReadOnlyList<AdminPermissionItem> Items { get; } =
        [
            new(AdminPermissionKeys.AdminAccess, "Admin Panel", "Index", false),
            new(AdminPermissionKeys.Products, "Products", "Products", true),
            new(AdminPermissionKeys.Categories, "Categories", "Categories", true),
            new(AdminPermissionKeys.Sliders, "Sliders", "Sliders", true),
            new(AdminPermissionKeys.ContactMessages, "Messages", "ContactMessages", true),
            new(AdminPermissionKeys.DeleteMessages, "Delete Messages", "ContactMessages", false),
            new(AdminPermissionKeys.Users, "Users", "Users", true),
            new(AdminPermissionKeys.Subscribe, "Subscribe", "Subscribe", true),
            new(AdminPermissionKeys.Roles, "Roles", "Roles", true),
            new(AdminPermissionKeys.FooterSettings, "Footer", "FooterSettings", true),
            new(AdminPermissionKeys.Refunds, "Refund", "Refunds", true),
            new(AdminPermissionKeys.Support, "Support", "Support", false)
        ];

        public static IReadOnlyList<AdminPermissionItem> NavItems { get; } =
        [
            new(AdminPermissionKeys.AdminAccess, "Dashboard", "Index", true),
            ..Items.Where(item => item.IsPage)
        ];

        public static string[] AllKeys { get; } = Items.Select(item => item.Key).ToArray();
    }
}
