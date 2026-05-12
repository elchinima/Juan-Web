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
    private readonly ImageStorageService _imageStorage;

    public HomeController(AppDbContext context, ImageStorageService imageStorage)
    {
        _context = context;
        _imageStorage = imageStorage;
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

    [Authorize]
    public async Task<IActionResult> SupportChat()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(await BuildSupportChatViewModelAsync(userId.Value));
    }

    [Authorize]
    public async Task<IActionResult> SupportChatHistory()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var tickets = await _context.SupportTickets
            .Where(ticket => ticket.UserId == userId.Value)
            .OrderByDescending(ticket => ticket.UpdatedAt)
            .ToListAsync();

        return View(tickets);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendSupportMessage(SupportMessageInput input)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var text = input.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) && input.ImageFile is not { Length: > 0 })
        {
            return RedirectToAction(nameof(SupportChat));
        }

        string? imageUrl = null;
        if (input.ImageFile is { Length: > 0 })
        {
            imageUrl = await _imageStorage.SaveSupportAttachmentAsWebpAsync(input.ImageFile);
        }

        var ticket = input.TicketId.HasValue
            ? await _context.SupportTickets.FirstOrDefaultAsync(item => item.Id == input.TicketId.Value && item.UserId == userId.Value)
            : await _context.SupportTickets
                .Where(item => item.UserId == userId.Value && item.Status != "Resolved")
                .OrderByDescending(item => item.UpdatedAt)
                .FirstOrDefaultAsync();

        if (ticket is null)
        {
            ticket = new SupportTicket
            {
                UserId = userId.Value,
                Code = $"PENDING-{Guid.NewGuid():N}"[..32],
                Subject = BuildSupportSubject(text, imageUrl),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            ticket.Code = $"SUP-{ticket.CreatedAt:yyyyMMdd}-{ticket.Id:D6}";
            _context.SupportTicketCreatedDates.Add(new SupportTicketCreatedDate
            {
                SupportTicketId = ticket.Id,
                CreatedAt = ticket.CreatedAt
            });
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        if (ticket.Status == "Resolved")
        {
            ticket.Status = "Open";
        }

        _context.SupportMessages.Add(new SupportMessage
        {
            SupportTicketId = ticket.Id,
            SenderUserId = userId.Value,
            IsOperator = false,
            Text = text,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(SupportChat));
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

    private async Task<SupportChatViewModel> BuildSupportChatViewModelAsync(int userId)
    {
        var ticket = await _context.SupportTickets
            .Include(item => item.OperatorUser)
            .Include(item => item.Messages)
            .ThenInclude(message => message.SenderUser)
            .Where(item => item.UserId == userId && item.Status != "Resolved")
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync();

        var isWaitingForOperator = ticket?.OperatorUser is null;
        var operatorInfo = isWaitingForOperator
            ? ("В ожидании", "Оператор подключается")
            : await GetSupportOperatorInfoAsync(ticket!.OperatorUser!);

        return new SupportChatViewModel
        {
            TicketId = ticket?.Id,
            TicketCode = ticket?.Code ?? string.Empty,
            OperatorFullName = operatorInfo.FullName,
            OperatorRole = operatorInfo.Role,
            IsWaitingForOperator = isWaitingForOperator,
            Messages = ticket?.Messages
                .OrderBy(message => message.CreatedAt)
                .Select(message => new SupportMessageViewModel
                {
                    SenderName = message.IsOperator ? operatorInfo.FullName : "You",
                    IsOperator = message.IsOperator,
                    Text = message.Text,
                    ImageUrl = message.ImageUrl,
                    CreatedAt = message.CreatedAt
                })
                .ToList() ?? []
        };
    }

    private async Task<(string FullName, string Role)> GetSupportOperatorInfoAsync(User operatorUser)
    {
        var role = await _context.UserAdminRoles
            .Where(userRole => userRole.UserId == operatorUser.Id && userRole.AdminRole.Permissions.Any(permission => permission.PermissionKey == AdminPermissionKeys.Support))
            .OrderBy(userRole => userRole.AdminRole.DisplayOrder)
            .Select(userRole => userRole.AdminRole.Name)
            .FirstOrDefaultAsync();

        return (operatorUser.FullName, role ?? "Support Operator");
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static string BuildSupportSubject(string? text, string? imageUrl)
    {
        var subject = string.IsNullOrWhiteSpace(text) ? "Image attachment" : text;
        if (!string.IsNullOrWhiteSpace(imageUrl) && string.IsNullOrWhiteSpace(text))
        {
            subject = "Image attachment";
        }

        return subject.Length <= 160 ? subject : subject[..160];
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
