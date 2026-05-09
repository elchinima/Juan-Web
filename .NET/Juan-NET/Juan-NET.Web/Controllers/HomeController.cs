using Juan_NET.Domain.Entities;
using Juan_NET.Persistence.Context;
using Juan_NET.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Juan_NET.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var viewModel = new HomeViewModel
        {
            Products = await _context.Products
                .Include(product => product.ProductCategories)
                .ThenInclude(productCategory => productCategory.Category)
                .Where(product => product.IsActive)
                .OrderByDescending(product => product.CreatedAt)
                .Take(8)
                .ToListAsync(),
            Sliders = await _context.Sliders
                .Where(slider => slider.IsActive)
                .OrderBy(slider => slider.DisplayOrder)
                .ThenBy(slider => slider.Id)
                .ToListAsync()
        };

        if (!viewModel.Sliders.Any())
        {
            viewModel.Sliders.AddRange(new[]
            {
                new Slider
                {
                    Subtitle = "Top Selling!",
                    Title = "New Collection",
                    Description = "Fresh styles from the latest Juan collection.",
                    ImageUrl = "/main assets/img/slider/slider-1.jpg",
                    ButtonText = "SHOP NOW",
                    ButtonUrl = "/Products"
                },
                new Slider
                {
                    Subtitle = "Best Selling!",
                    Title = "Top Collection",
                    Description = "Shop the favorite products from our catalog.",
                    ImageUrl = "/main assets/img/slider/slider-2.jpg",
                    ButtonText = "SHOP NOW",
                    ButtonUrl = "/Products"
                }
            });
        }

        return View(viewModel);
    }

    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubscribeEmail(string email)
    {
        if (!string.IsNullOrWhiteSpace(email) && !await _context.Subscribers.AnyAsync(subscriber => subscriber.Email == email))
        {
            _context.Subscribers.Add(new Subscriber
            {
                Email = email.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["SubscribeMessage"] = "Thank you for subscribing.";
        }

        var returnUrl = Request.Headers.Referer.ToString();
        return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }
}
