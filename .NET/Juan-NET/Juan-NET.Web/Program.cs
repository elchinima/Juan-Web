var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5219");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ImageStorageService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AdminAccessService>();
builder.Services.AddScoped<SupportWorkTimeService>();
builder.Services.AddHostedService<SupportReportCleanupService>();
builder.Services.AddHostedService<FavoriteCategoryDigestService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });
builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/View/{1}/{0}.cshtml");
    options.ViewLocationFormats.Add("/View/Shared/{0}.cshtml");
});

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AdminAccessService.EnsureRoleInfrastructureAsync(context);
    await FavoriteCategoryInfrastructureService.EnsureInfrastructureAsync(context);
    await ShopListInfrastructureService.EnsureInfrastructureAsync(context);
    await SiteSettingsInfrastructureService.EnsureInfrastructureAsync(context);
    await OrderInfrastructureService.EnsureInfrastructureAsync(context);
    await SupportInfrastructureService.EnsureInfrastructureAsync(context);
    await ProductReviewInfrastructureService.EnsureInfrastructureAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler("/Error/500");
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = [new CultureInfo("en-US")],
    SupportedUICultures = [new CultureInfo("en-US")]
});
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(userIdValue, out var userId))
        {
            var isSupportPath = context.Request.Path.StartsWithSegments("/Support", StringComparison.OrdinalIgnoreCase);
            var adminAccess = context.RequestServices.GetRequiredService<AdminAccessService>();

            if (await adminAccess.HasPermissionAsync(context.User, AdminPermissionKeys.Support))
            {
                var supportWorkTime = context.RequestServices.GetRequiredService<SupportWorkTimeService>();
                await supportWorkTime.UpdateShiftAsync(userId, isSupportPath);
            }
        }
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
