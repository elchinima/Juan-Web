namespace Juan_NET.Infrastructure.Database
{
    public static class DatabaseInfrastructureInitializer
    {
        public static async Task EnsureInfrastructureAsync(AppDbContext context)
        {
            await AdminAccessService.EnsureRoleInfrastructureAsync(context);
            await FavoriteCategoryInfrastructureService.EnsureInfrastructureAsync(context);
            await ShopListInfrastructureService.EnsureInfrastructureAsync(context);
            await SiteSettingsInfrastructureService.EnsureInfrastructureAsync(context);
            await OrderInfrastructureService.EnsureInfrastructureAsync(context);
            await SupportInfrastructureService.EnsureInfrastructureAsync(context);
            await ProductReviewInfrastructureService.EnsureInfrastructureAsync(context);
        }
    }
}
