namespace Juan_NET.Web.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
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

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetCurrentUserId();

                if (userId.HasValue)
                {
                    var favoriteCategoryIds = await _context.UserFavoriteCategories
                        .Where(favorite => favorite.UserId == userId.Value)
                        .Select(favorite => favorite.CategoryId)
                        .ToListAsync();
                    var favoriteCategoryIdSet = favoriteCategoryIds.ToHashSet();

                    foreach (var category in categories)
                    {
                        category.IsFavorite = favoriteCategoryIdSet.Contains(category.Id);
                    }
                }
            }

            return View(new CategoriesViewModel
            {
                Categories = categories,
                IsAuthenticated = User.Identity?.IsAuthenticated == true
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue || !await _context.Categories.AnyAsync(category => category.Id == id))
            {
                return RedirectToAction(nameof(Index));
            }

            var favorite = await _context.UserFavoriteCategories.FindAsync(userId.Value, id);

            if (favorite is null)
            {
                _context.UserFavoriteCategories.Add(new UserFavoriteCategory
                {
                    UserId = userId.Value,
                    CategoryId = id
                });
            }
            else
            {
                _context.UserFavoriteCategories.Remove(favorite);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out var id) ? id : null;
        }
    }
}
