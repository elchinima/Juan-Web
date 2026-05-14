namespace Juan_NET.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ImageStorageService>();
            services.AddScoped<EmailService>();
            services.AddScoped<AdminAccessService>();
            services.AddScoped<SupportWorkTimeService>();
            services.AddHostedService<SupportReportCleanupService>();
            services.AddHostedService<FavoriteCategoryDigestService>();

            return services;
        }
    }
}
