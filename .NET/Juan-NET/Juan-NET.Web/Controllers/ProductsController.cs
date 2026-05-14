
namespace Juan_NET.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AdminAccessService _adminAccess;

        public ProductsController(AppDbContext context, AdminAccessService adminAccess)
        {
            _context = context;
            _adminAccess = adminAccess;
        }

        public async Task<IActionResult> Index(string? query, int? categoryId, decimal? minPrice, decimal? maxPrice, decimal? minRating, int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(1, page);

            IQueryable<Product> productsQuery = _context.Products
                .Include(product => product.ProductCategories)
                .ThenInclude(productCategory => productCategory.Category)
                .Where(product => product.IsActive);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var searchTerm = query.Trim();
                productsQuery = productsQuery.Where(product => product.Name.Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.ProductCategories.Any(productCategory => productCategory.CategoryId == categoryId.Value));
            }

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.Price <= maxPrice.Value);
            }

            if (minRating.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.Reviews.Any() && product.Reviews.Average(review => review.Rating) >= minRating.Value);
            }

            productsQuery = productsQuery.OrderByDescending(product => product.CreatedAt);

            var totalProducts = await productsQuery.CountAsync();
            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var productIds = products.Select(product => product.Id).ToList();
            var currentUserId = GetCurrentUserId();
            var canManageProductReviews = await _adminAccess.HasPermissionAsync(User, AdminPermissionKeys.Products);
            var reviews = productIds.Count == 0
                ? new List<ProductReviewViewModel>()
                : await _context.ProductReviews
                    .Include(review => review.User)
                    .Where(review => productIds.Contains(review.ProductId))
                    .OrderByDescending(review => review.CreatedAt)
                    .Select(review => new ProductReviewViewModel
                    {
                        Id = review.Id,
                        ProductId = review.ProductId,
                        UserId = review.UserId,
                        UserName = review.User.FullName,
                        UserImageUrl = review.User.ProfileImageUrl,
                        Rating = review.Rating,
                        Comment = review.Comment,
                        IsVerifiedPurchase = review.IsVerifiedPurchase,
                        CreatedAt = review.CreatedAt,
                        CanDelete = canManageProductReviews || review.UserId == currentUserId
                    })
                    .ToListAsync();
            var summaries = reviews
                .GroupBy(review => review.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group => new ProductReviewSummaryViewModel
                    {
                        Count = group.Count(),
                        AverageRating = Math.Round(group.Average(review => review.Rating), 1, MidpointRounding.AwayFromZero)
                    });
            var categories = await _context.Categories
                .Select(category => new CategoryCardViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    ProductCount = category.ProductCategories.Count(productCategory => productCategory.Product.IsActive)
                })
                .Where(category => category.ProductCount > 0)
                .OrderBy(category => category.Name)
                .ToListAsync();

            return View(new ProductsIndexViewModel
            {
                Products = products,
                CurrentPage = page,
                HasPreviousPage = page > 1,
                HasNextPage = page * pageSize < totalProducts,
                ReviewsByProductId = reviews
                    .GroupBy(review => review.ProductId)
                    .ToDictionary(group => group.Key, group => group.ToList()),
                ReviewSummariesByProductId = summaries,
                Categories = categories,
                SearchTerm = query,
                CategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(ProductReviewInput input)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var rating = Math.Round(input.Rating, 1, MidpointRounding.AwayFromZero);
            if (rating < 1.0m || rating > 5.0m)
            {
                return RedirectToProducts();
            }

            var productExists = await _context.Products.AnyAsync(product => product.Id == input.ProductId && product.IsActive);
            if (!productExists)
            {
                return RedirectToProducts();
            }

            var isVerifiedPurchase = await _context.OrderItems
                .AnyAsync(item => item.ProductId == input.ProductId &&
                    item.Order.UserId == userId &&
                    item.Order.Status == "Paid");
            var comment = string.IsNullOrWhiteSpace(input.Comment) ? null : input.Comment.Trim();

            if (comment is { Length: > 1000 })
            {
                comment = comment[..1000];
            }

            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(item => item.ProductId == input.ProductId && item.UserId == userId);

            if (review is null)
            {
                _context.ProductReviews.Add(new ProductReview
                {
                    ProductId = input.ProductId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    IsVerifiedPurchase = isVerifiedPurchase,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                review.Rating = rating;
                review.Comment = comment;
                review.IsVerifiedPurchase = isVerifiedPurchase;
                review.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToProducts();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var review = await _context.ProductReviews.FindAsync(id);
            if (review is null)
            {
                return RedirectToProducts();
            }

            var canManageProductReviews = await _adminAccess.HasPermissionAsync(User, AdminPermissionKeys.Products);
            if (review.UserId != userId.Value && !canManageProductReviews)
            {
                return Forbid();
            }

            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();

            return RedirectToProducts();
        }

        private IActionResult RedirectToProducts()
        {
            var returnUrl = Request.Headers.Referer.ToString();
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
