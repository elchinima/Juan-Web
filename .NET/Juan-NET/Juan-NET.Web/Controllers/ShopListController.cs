namespace Juan_NET.Web.Controllers
{
    public class ShopListController : Controller
    {
        private readonly AppDbContext _context;

        public ShopListController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Summary()
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Json(new ShopListSummaryViewModel());
            }

            return Json(await BuildSummaryAsync(userId.Value));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBasket(int productId, int quantity = 1)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var product = await _context.Products.FirstOrDefaultAsync(item => item.Id == productId && item.IsActive);
            if (product is null)
            {
                return NotFound();
            }

            if (product.StockCount < 1)
            {
                return BadRequest();
            }

            var maxQuantity = Math.Min(product.StockCount, 99);
            quantity = Math.Clamp(quantity, 1, maxQuantity);
            var item = await _context.BasketItems.FindAsync(userId.Value, productId);

            if (item is null)
            {
                _context.BasketItems.Add(new BasketItem
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                item.Quantity = Math.Clamp(item.Quantity + quantity, 1, maxQuantity);
            }

            await _context.SaveChangesAsync();
            return Json(await BuildSummaryAsync(userId.Value));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWishlist(int productId)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            var productExists = await _context.Products.AnyAsync(item => item.Id == productId && item.IsActive);
            if (!productExists)
            {
                return NotFound();
            }

            var item = await _context.WishlistItems.FindAsync(userId.Value, productId);
            if (item is null)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Json(await BuildSummaryAsync(userId.Value));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBasket(int productId)
        {
            var userId = GetUserId();
            var item = userId is null ? null : await _context.BasketItems.FindAsync(userId.Value, productId);

            if (item is not null)
            {
                _context.BasketItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Json(userId is null ? new ShopListSummaryViewModel() : await BuildSummaryAsync(userId.Value));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveWishlist(int productId)
        {
            var userId = GetUserId();
            var item = userId is null ? null : await _context.WishlistItems.FindAsync(userId.Value, productId);

            if (item is not null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Json(userId is null ? new ShopListSummaryViewModel() : await BuildSummaryAsync(userId.Value));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sync([FromBody] ShopListSyncInput input)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }

            foreach (var localItem in input.BasketItems.Where(item => item.ProductId > 0))
            {
                var product = await _context.Products.FirstOrDefaultAsync(item => item.Id == localItem.ProductId && item.IsActive);
                if (product is null)
                {
                    continue;
                }

                if (product.StockCount < 1)
                {
                    continue;
                }

                var maxQuantity = Math.Min(product.StockCount, 99);
                var quantity = Math.Clamp(localItem.Quantity, 1, maxQuantity);
                var basketItem = await _context.BasketItems.FindAsync(userId.Value, localItem.ProductId);
                if (basketItem is null)
                {
                    _context.BasketItems.Add(new BasketItem { UserId = userId.Value, ProductId = localItem.ProductId, Quantity = quantity });
                }
                else
                {
                    basketItem.Quantity = Math.Clamp(basketItem.Quantity + quantity, 1, maxQuantity);
                }
            }

            foreach (var localItem in input.WishlistItems.Where(item => item.ProductId > 0))
            {
                var productExists = await _context.Products.AnyAsync(item => item.Id == localItem.ProductId && item.IsActive);
                var exists = await _context.WishlistItems.FindAsync(userId.Value, localItem.ProductId) is not null;
                if (productExists && !exists)
                {
                    _context.WishlistItems.Add(new WishlistItem { UserId = userId.Value, ProductId = localItem.ProductId });
                }
            }

            await _context.SaveChangesAsync();
            return Json(await BuildSummaryAsync(userId.Value));
        }

        private async Task<ShopListSummaryViewModel> BuildSummaryAsync(int userId)
        {
            return new ShopListSummaryViewModel
            {
                BasketItems = await _context.BasketItems
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
                    .ToListAsync(),
                WishlistItems = await _context.WishlistItems
                    .Where(item => item.UserId == userId && item.Product.IsActive)
                    .OrderByDescending(item => item.CreatedAt)
                    .Select(item => new ShopListItemViewModel
                    {
                        ProductId = item.ProductId,
                        Name = item.Product.Name,
                        ImageUrl = item.Product.ImageUrl ?? "/main assets/img/product/product-1.jpg",
                        Price = item.Product.Price,
                        Quantity = 1,
                        StockCount = item.Product.StockCount
                    })
                    .ToListAsync()
            };
        }

        private int? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out var id) ? id : null;
        }
    }

    public class ShopListSyncInput
    {
        public List<LocalShopItemInput> BasketItems { get; set; } = new();

        public List<LocalShopItemInput> WishlistItems { get; set; } = new();
    }
}
