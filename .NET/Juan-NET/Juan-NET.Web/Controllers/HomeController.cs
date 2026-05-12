using Juan_NET.Domain.Entities;
using Juan_NET.Persistence.Context;
using Juan_NET.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
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
        return View(new ContactMessageViewModel());
    }

    public IActionResult SupportChat()
    {
        return View();
    }

    public IActionResult SupportChatHistory()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> SupportChatOrders()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var orders = await _context.Orders
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new ProfileOrderViewModel
            {
                Id = order.Id,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                Currency = order.Currency,
                Subtotal = order.Subtotal,
                DeliveryTotal = order.DeliveryTotal,
                DiscountTotal = order.DiscountTotal,
                Total = order.Total,
                PromoCode = order.PromoCode,
                Items = order.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new ProfileOrderItemViewModel
                    {
                        ProductName = item.ProductName,
                        ImageUrl = item.ProductImageUrl ?? "/main assets/img/product/product-1.jpg",
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        UnitDeliveryPrice = item.UnitDeliveryPrice,
                        LineTotal = item.LineTotal
                    })
                    .ToList()
            })
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactMessageViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.OpenComplaintModal = true;
            return View(viewModel);
        }

        _context.ContactMessages.Add(new ContactMessage
        {
            Name = viewModel.Name.Trim(),
            Email = viewModel.Email.Trim(),
            Message = viewModel.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["ContactMessage"] = "Your message has been sent.";

        return RedirectToAction(nameof(Contact));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int productId, string quantity)
    {
        if (!int.TryParse(quantity, out var parsedQuantity) || parsedQuantity < 1 || parsedQuantity > 99)
        {
            TempData["CartMessage"] = "Quantity must be a whole number from 1 to 99.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _context.Products
            .Where(product => product.IsActive)
            .FirstOrDefaultAsync(product => product.Id == productId);

        if (product is null)
        {
            TempData["CartMessage"] = "Product was not found.";
            return RedirectToAction(nameof(Index));
        }

        if (parsedQuantity > product.StockCount)
        {
            TempData["CartMessage"] = "Quantity cannot be greater than stock.";
            return RedirectToAction(nameof(Index));
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userId, out var id))
        {
            var cartItem = await _context.BasketItems.FindAsync(id, productId);
            var quantityToAdd = Math.Clamp(parsedQuantity, 1, Math.Min(product.StockCount, 99));

            if (cartItem is null)
            {
                _context.BasketItems.Add(new BasketItem
                {
                    UserId = id,
                    ProductId = productId,
                    Quantity = quantityToAdd,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                cartItem.Quantity = Math.Clamp(cartItem.Quantity + quantityToAdd, 1, Math.Min(product.StockCount, 99));
            }

            await _context.SaveChangesAsync();
        }

        TempData["CartMessage"] = "Product added to basket.";
        return RedirectToAction(nameof(Index));
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
