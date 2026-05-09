using System.Globalization;
using Juan_NET.Domain.Entities;
using Juan_NET.Persistence.Context;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/View/{1}/{0}.cshtml");
    options.ViewLocationFormats.Add("/View/Shared/{0}.cshtml");
});

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    dbContext.Database.ExecuteSqlRaw("""
        IF OBJECT_ID(N'[Categories]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Categories] (
                [Id] int NOT NULL IDENTITY,
                [Name] nvarchar(80) NOT NULL,
                CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
        END

        IF OBJECT_ID(N'[ProductCategories]', N'U') IS NULL
        BEGIN
            CREATE TABLE [ProductCategories] (
                [ProductId] int NOT NULL,
                [CategoryId] int NOT NULL,
                CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([ProductId], [CategoryId]),
                CONSTRAINT [FK_ProductCategories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_ProductCategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_ProductCategories_CategoryId] ON [ProductCategories] ([CategoryId]);
        END

        IF OBJECT_ID(N'[Sliders]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Sliders] (
                [Id] int NOT NULL IDENTITY,
                [Subtitle] nvarchar(80) NOT NULL,
                [Title] nvarchar(120) NOT NULL,
                [Description] nvarchar(300) NOT NULL,
                [ImageUrl] nvarchar(300) NOT NULL,
                [ButtonText] nvarchar(80) NOT NULL,
                [ButtonUrl] nvarchar(300) NOT NULL,
                [DisplayOrder] int NOT NULL,
                [IsActive] bit NOT NULL,
                CONSTRAINT [PK_Sliders] PRIMARY KEY ([Id])
            );
        END
        """);

    if (!dbContext.Categories.Any())
    {
        dbContext.Categories.AddRange(
            new Category { Name = "Shoes" },
            new Category { Name = "Bags" },
            new Category { Name = "Accessories" },
            new Category { Name = "Clothing" });
        dbContext.SaveChanges();
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
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
