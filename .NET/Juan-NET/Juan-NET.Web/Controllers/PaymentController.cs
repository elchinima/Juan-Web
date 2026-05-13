using Stripe;
using Stripe.Checkout;

namespace Juan_NET.Web.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly StripeSettings _stripeSettings;

        public PaymentController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _stripeSettings = configuration.GetSection("Stripe").Get<StripeSettings>() ?? new StripeSettings();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Challenge();
            }

            var viewModel = new CheckoutViewModel
            {
                Items = await BuildBasketAsync(userId.Value),
                PublishableKey = _stripeSettings.PublishableKey,
                Currency = _stripeSettings.Currency,
                IsStripeConfigured = _stripeSettings.IsConfigured,
                HasDeliveryInformation = await HasDeliveryInformationAsync(userId.Value)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(string? promoCode)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized(new { message = "Please sign in before checkout." });
            }

            var user = await _context.Users
                .Include(item => item.Addresses)
                .FirstOrDefaultAsync(item => item.Id == userId.Value);
            if (user is null)
            {
                return Unauthorized(new { message = "Please sign in before checkout." });
            }

            if (!HasDeliveryInformation(user))
            {
                return BadRequest(new { message = "Add delivery information before checkout." });
            }

            if (!_stripeSettings.IsConfigured)
            {
                return BadRequest(new { message = "Stripe keys are not configured." });
            }

            var basketItems = await _context.BasketItems
                .Include(item => item.Product)
                .Where(item => item.UserId == userId.Value && item.Product.IsActive && item.Product.StockCount > 0)
                .OrderByDescending(item => item.CreatedAt)
                .ToListAsync();

            if (!basketItems.Any())
            {
                return BadRequest(new { message = "Basket is empty." });
            }

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            var normalizedPromoCode = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim();
            var promotionCodeId = await FindPromotionCodeIdAsync(normalizedPromoCode);

            if (!string.IsNullOrWhiteSpace(normalizedPromoCode) && string.IsNullOrWhiteSpace(promotionCodeId))
            {
                return BadRequest(new { message = "Promo code was not found." });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sessionOptions = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{baseUrl}{Url.Action(nameof(Success), "Payment")}?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{baseUrl}{Url.Action(nameof(Cancel), "Payment")}",
                ClientReferenceId = userId.Value.ToString(CultureInfo.InvariantCulture),
                CustomerEmail = user.Email,
                AllowPromotionCodes = string.IsNullOrWhiteSpace(promotionCodeId),
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.Value.ToString(CultureInfo.InvariantCulture),
                    ["basket"] = string.Join(",", basketItems.Select(item => $"{item.ProductId}:{Math.Min(item.Quantity, item.Product.StockCount)}")),
                    ["promoCode"] = normalizedPromoCode ?? string.Empty
                },
                Discounts = string.IsNullOrWhiteSpace(promotionCodeId)
                    ? null
                    : new List<SessionDiscountOptions> { new() { PromotionCode = promotionCodeId } },
                LineItems = basketItems.SelectMany(item => new[]
                {
                    new SessionLineItemOptions
                    {
                        Quantity = Math.Min(item.Quantity, item.Product.StockCount),
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = NormalizeCurrency(_stripeSettings.Currency),
                            UnitAmount = ToStripeAmount(item.Product.Price),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Name,
                                Images = string.IsNullOrWhiteSpace(item.Product.ImageUrl)
                                    ? null
                                    : new List<string> { BuildAbsoluteUrl(item.Product.ImageUrl) }
                            }
                        }
                    },
                    new SessionLineItemOptions
                    {
                        Quantity = Math.Min(item.Quantity, item.Product.StockCount),
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = NormalizeCurrency(_stripeSettings.Currency),
                            UnitAmount = ToStripeAmount(GetDeliveryPrice(item.Product.Price)),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Delivery - {item.Product.Name}"
                            }
                        }
                    }
                }).ToList()
            };

            var session = await new SessionService().CreateAsync(sessionOptions);
            return Json(new { sessionId = session.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Success(string? session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
            {
                TempData["PaymentMessage"] = "Payment session was not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!_stripeSettings.IsConfigured)
            {
                TempData["PaymentMessage"] = "Stripe keys are not configured.";
                return RedirectToAction(nameof(Index));
            }

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            var session = await new SessionService().GetAsync(session_id);

            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.PaymentStatus = session.PaymentStatus;
                ViewBag.SessionId = session.Id;
                return View();
            }

            var userId = GetUserId();
            if (userId is not null &&
                string.Equals(session.ClientReferenceId, userId.Value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                await CompletePaidBasketAsync(userId.Value, session);
            }

            ViewBag.PaymentStatus = session.PaymentStatus;
            ViewBag.SessionId = session.Id;
            ViewBag.AmountTotal = session.AmountTotal is null ? null : session.AmountTotal / 100m;
            ViewBag.Currency = session.Currency?.ToUpperInvariant();
            return View();
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }

        private async Task<List<ShopListItemViewModel>> BuildBasketAsync(int userId)
        {
            return await _context.BasketItems
                .Where(item => item.UserId == userId && item.Product.IsActive)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new ShopListItemViewModel
                {
                    ProductId = item.ProductId,
                    Name = item.Product.Name,
                    ImageUrl = item.Product.ImageUrl ?? "/main assets/img/product/product-1.jpg",
                    Price = item.Product.Price,
                    Quantity = item.Quantity,
                    StockCount = item.Product.StockCount
                })
                .ToListAsync();
        }

        private async Task CompletePaidBasketAsync(int userId, Session session)
        {
            var paidItems = ParseBasketMetadata(session.Metadata);
            if (!paidItems.Any())
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(session.Id) && await _context.Orders.AnyAsync(order => order.StripeSessionId == session.Id))
            {
                return;
            }

            var user = await _context.Users
                .Include(item => item.Addresses)
                .FirstOrDefaultAsync(item => item.Id == userId);
            if (user is null || !HasDeliveryInformation(user))
            {
                return;
            }

            var address = GetDefaultAddress(user)!;

            var basketItems = await _context.BasketItems
                .Include(item => item.Product)
                .Where(item => item.UserId == userId && paidItems.Keys.Contains(item.ProductId))
                .ToListAsync();

            if (!basketItems.Any())
            {
                return;
            }

            var order = new Order
            {
                UserId = userId,
                RecipientFullName = address.RecipientFullName.Trim(),
                AddressLine1 = address.AddressLine1.Trim(),
                AddressLine2 = string.IsNullOrWhiteSpace(address.AddressLine2) ? null : address.AddressLine2.Trim(),
                Fin = address.Fin.Trim().ToUpperInvariant(),
                StripeSessionId = session.Id,
                StripePaymentIntentId = session.PaymentIntentId,
                PromoCode = session.Metadata is not null && session.Metadata.TryGetValue("promoCode", out var promoCode) && !string.IsNullOrWhiteSpace(promoCode) ? promoCode : null,
                Currency = NormalizeCurrency(session.Currency ?? _stripeSettings.Currency),
                Status = "Paid",
                DiscountTotal = ToDecimalAmount(session.TotalDetails?.AmountDiscount),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in basketItems)
            {
                var paidQuantity = paidItems[item.ProductId];
                var quantity = Math.Min(item.Quantity, paidQuantity);
                var unitDeliveryPrice = GetDeliveryPrice(item.Product.Price);
                var lineTotal = (item.Product.Price + unitDeliveryPrice) * quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductImageUrl = item.Product.ImageUrl,
                    UnitPrice = item.Product.Price,
                    UnitDeliveryPrice = unitDeliveryPrice,
                    Quantity = quantity,
                    LineTotal = lineTotal
                });

                order.Subtotal += item.Product.Price * quantity;
                order.DeliveryTotal += unitDeliveryPrice * quantity;
                item.Product.StockCount = Math.Max(0, item.Product.StockCount - quantity);
                _context.BasketItems.Remove(item);
            }

            order.Total = Math.Max(0, order.Subtotal + order.DeliveryTotal - order.DiscountTotal);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
        }

        private async Task<string?> FindPromotionCodeIdAsync(string? promoCode)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return null;
            }

            var service = new PromotionCodeService();
            var result = await service.ListAsync(new PromotionCodeListOptions
            {
                Code = promoCode,
                Active = true,
                Limit = 1
            });

            return result.Data.FirstOrDefault()?.Id;
        }

        private async Task<bool> HasDeliveryInformationAsync(int userId)
        {
            var user = await _context.Users
                .Include(item => item.Addresses)
                .FirstOrDefaultAsync(item => item.Id == userId);
            return user is not null && HasDeliveryInformation(user);
        }

        private static bool HasDeliveryInformation(User user)
        {
            var address = GetDefaultAddress(user);

            return address is not null &&
                !string.IsNullOrWhiteSpace(address.RecipientFullName) &&
                !string.IsNullOrWhiteSpace(address.AddressLine1) &&
                !string.IsNullOrWhiteSpace(address.Fin) &&
                address.Fin.Trim().Length == 7;
        }

        private static UserAddress? GetDefaultAddress(User user)
        {
            return user.Addresses
                .OrderByDescending(address => address.IsDefault)
                .ThenBy(address => address.Id)
                .FirstOrDefault();
        }

        private static decimal GetDeliveryPrice(decimal productPrice)
        {
            return Math.Round(productPrice * 0.10m, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal ToDecimalAmount(long? amount)
        {
            return amount is null ? 0m : amount.Value / 100m;
        }

        private static Dictionary<int, int> ParseBasketMetadata(Dictionary<string, string>? metadata)
        {
            if (metadata is null || !metadata.TryGetValue("basket", out var value))
            {
                return new Dictionary<int, int>();
            }

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Split(':', StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length == 2 &&
                    int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out _) &&
                    int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                .Select(parts => new
                {
                    ProductId = int.Parse(parts[0], CultureInfo.InvariantCulture),
                    Quantity = int.Parse(parts[1], CultureInfo.InvariantCulture)
                })
                .Where(item => item.ProductId > 0 && item.Quantity > 0)
                .ToDictionary(item => item.ProductId, item => item.Quantity);
        }

        private string BuildAbsoluteUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
            {
                return absoluteUrl.ToString();
            }

            return $"{Request.Scheme}://{Request.Host}{Url.Content(url.StartsWith('/') ? $"~{url}" : $"~/{url}")}";
        }

        private static string NormalizeCurrency(string currency)
        {
            return string.IsNullOrWhiteSpace(currency) ? "usd" : currency.Trim().ToLowerInvariant();
        }

        private static long ToStripeAmount(decimal amount)
        {
            return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        }

        private int? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out var id) ? id : null;
        }
    }
}
