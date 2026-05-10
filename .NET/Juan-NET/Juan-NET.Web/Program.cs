var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5219");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ImageStorageService>();
builder.Services.AddScoped<EmailService>();
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
    try
    {
        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[ContactMessages]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ContactMessages] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(120) NOT NULL,
                    [Email] nvarchar(180) NOT NULL,
                    [Message] nvarchar(1000) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
                    CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
                );
            END
            """);
    }
    catch (Exception exception)
    {
        Console.WriteLine($"ContactMessages table check failed: {exception.Message}");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = [new CultureInfo("en-US")],
    SupportedUICultures = [new CultureInfo("en-US")]
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
