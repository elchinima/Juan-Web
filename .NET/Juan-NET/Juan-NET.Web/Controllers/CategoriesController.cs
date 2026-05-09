using Juan_NET.Persistence.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var categories = await _context.Products
                .Where(product => product.IsActive)
                .SelectMany(product => product.ProductCategories.Select(productCategory => productCategory.Category.Name))
                .Distinct()
                .OrderBy(category => category)
                .ToListAsync();

            return View(categories);
        }
    }
}
