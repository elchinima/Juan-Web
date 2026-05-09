using Juan_NET.Domain.Entities;
using Juan_NET.Persistence.Context;
using Juan_NET.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Juan_NET.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ImageStorageService _imageStorage;

        public AdminController(AppDbContext context, ImageStorageService imageStorage)
        {
            _context = context;
            _imageStorage = imageStorage;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ProductCount = await _context.Products.CountAsync();
            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.CategoryCount = await _context.Categories.CountAsync();
            ViewBag.SliderCount = await _context.Sliders.CountAsync();
            return View();
        }

        public async Task<IActionResult> Products()
        {
            var viewModel = new AdminProductsViewModel
            {
                Products = await _context.Products
                    .Include(product => product.ProductCategories)
                    .ThenInclude(productCategory => productCategory.Category)
                    .OrderByDescending(product => product.CreatedAt)
                    .ToListAsync(),
                Categories = await _context.Categories.OrderBy(category => category.Name).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(AdminProductsViewModel viewModel)
        {
            ModelState.Remove(nameof(AdminProductsViewModel.Products));
            ModelState.Remove(nameof(AdminProductsViewModel.Categories));
            ModelState.Remove($"{nameof(AdminProductsViewModel.Product)}.{nameof(Product.CategoryName)}");
            ModelState.Remove($"{nameof(AdminProductsViewModel.Product)}.{nameof(Product.ProductCategories)}");

            viewModel.SelectedCategoryIds = viewModel.SelectedCategoryIds.Distinct().ToList();

            if (viewModel.SelectedCategoryIds.Count < 1 || viewModel.SelectedCategoryIds.Count > 3)
            {
                ModelState.AddModelError(nameof(AdminProductsViewModel.SelectedCategoryIds), "Select from 1 to 3 categories.");
            }

            if (!ModelState.IsValid)
            {
                viewModel.Products = await _context.Products
                    .Include(product => product.ProductCategories)
                    .ThenInclude(productCategory => productCategory.Category)
                    .OrderByDescending(product => product.CreatedAt)
                    .ToListAsync();
                viewModel.Categories = await _context.Categories.OrderBy(category => category.Name).ToListAsync();
                return View("Products", viewModel);
            }

            var selectedCategories = await _context.Categories
                .Where(category => viewModel.SelectedCategoryIds.Contains(category.Id))
                .OrderBy(category => category.Name)
                .ToListAsync();

            if (selectedCategories.Count != viewModel.SelectedCategoryIds.Count)
            {
                ModelState.AddModelError(nameof(AdminProductsViewModel.SelectedCategoryIds), "Selected categories are invalid.");
                viewModel.Products = await _context.Products
                    .Include(product => product.ProductCategories)
                    .ThenInclude(productCategory => productCategory.Category)
                    .OrderByDescending(product => product.CreatedAt)
                    .ToListAsync();
                viewModel.Categories = await _context.Categories.OrderBy(category => category.Name).ToListAsync();
                return View("Products", viewModel);
            }

            var product = viewModel.Product;
            product.CreatedAt = DateTime.UtcNow;
            product.CategoryName = string.Join(", ", selectedCategories.Select(category => category.Name));

            if (viewModel.ImageFile is { Length: > 0 })
            {
                product.ImageUrl = await _imageStorage.SaveAsWebpAsync(viewModel.ImageFile, "products");
            }
            else
            {
                product.ImageUrl = "/main assets/img/product/product-1.jpg";
            }

            foreach (var category in selectedCategories)
            {
                product.ProductCategories.Add(new ProductCategory { CategoryId = category.Id });
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            TempData["ProductMessage"] = "Product added successfully.";

            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is not null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["ProductMessage"] = "Product deleted successfully.";
            }

            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> Categories()
        {
            var viewModel = new AdminCategoriesViewModel
            {
                Categories = await _context.Categories
                    .Include(category => category.ProductCategories)
                    .OrderBy(category => category.Name)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(AdminCategoriesViewModel viewModel)
        {
            ModelState.Remove(nameof(AdminCategoriesViewModel.Categories));
            ModelState.Remove($"{nameof(AdminCategoriesViewModel.Category)}.{nameof(Category.ProductCategories)}");

            var name = viewModel.Category.Name.Trim();

            if (await _context.Categories.AnyAsync(category => category.Name == name))
            {
                ModelState.AddModelError($"{nameof(AdminCategoriesViewModel.Category)}.{nameof(Category.Name)}", "Category already exists.");
            }

            if (!ModelState.IsValid)
            {
                viewModel.Categories = await _context.Categories
                    .Include(category => category.ProductCategories)
                    .OrderBy(category => category.Name)
                    .ToListAsync();
                return View("Categories", viewModel);
            }

            _context.Categories.Add(new Category { Name = name });
            await _context.SaveChangesAsync();
            TempData["CategoryMessage"] = "Category added successfully.";

            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category is not null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["CategoryMessage"] = "Category deleted successfully.";
            }

            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Sliders()
        {
            var viewModel = new AdminSlidersViewModel
            {
                Sliders = await _context.Sliders
                    .OrderBy(slider => slider.DisplayOrder)
                    .ThenBy(slider => slider.Id)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSlider(AdminSlidersViewModel viewModel)
        {
            ModelState.Remove(nameof(AdminSlidersViewModel.Sliders));

            if (!ModelState.IsValid)
            {
                viewModel.Sliders = await _context.Sliders.OrderBy(slider => slider.DisplayOrder).ToListAsync();
                return View("Sliders", viewModel);
            }

            var slider = viewModel.Slider;

            if (viewModel.ImageFile is { Length: > 0 })
            {
                slider.ImageUrl = await _imageStorage.SaveAsWebpAsync(viewModel.ImageFile, "sliders");
            }

            _context.Sliders.Add(slider);
            await _context.SaveChangesAsync();
            TempData["SliderMessage"] = "Slider added successfully.";

            return RedirectToAction(nameof(Sliders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSlider(AdminSlidersViewModel viewModel)
        {
            ModelState.Remove(nameof(AdminSlidersViewModel.Sliders));

            var slider = await _context.Sliders.FindAsync(viewModel.Slider.Id);

            if (slider is null)
            {
                return RedirectToAction(nameof(Sliders));
            }

            if (!ModelState.IsValid)
            {
                viewModel.Sliders = await _context.Sliders.OrderBy(item => item.DisplayOrder).ToListAsync();
                return View("Sliders", viewModel);
            }

            slider.Subtitle = viewModel.Slider.Subtitle;
            slider.Title = viewModel.Slider.Title;
            slider.Description = viewModel.Slider.Description;
            slider.ButtonText = viewModel.Slider.ButtonText;
            slider.ButtonUrl = viewModel.Slider.ButtonUrl;
            slider.DisplayOrder = viewModel.Slider.DisplayOrder;
            slider.IsActive = viewModel.Slider.IsActive;

            if (viewModel.ImageFile is { Length: > 0 })
            {
                slider.ImageUrl = await _imageStorage.SaveAsWebpAsync(viewModel.ImageFile, "sliders");
            }
            else if (!string.IsNullOrWhiteSpace(viewModel.Slider.ImageUrl))
            {
                slider.ImageUrl = viewModel.Slider.ImageUrl;
            }

            await _context.SaveChangesAsync();
            TempData["SliderMessage"] = "Slider updated successfully.";

            return RedirectToAction(nameof(Sliders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSlider(int id)
        {
            var slider = await _context.Sliders.FindAsync(id);

            if (slider is not null)
            {
                _context.Sliders.Remove(slider);
                await _context.SaveChangesAsync();
                TempData["SliderMessage"] = "Slider deleted successfully.";
            }

            return RedirectToAction(nameof(Sliders));
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.OrderByDescending(user => user.CreatedAt).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Subscribe()
        {
            return View(await CreateSubscribeViewModelAsync(new AdminSubscribeViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSubscribe(AdminSubscribeViewModel viewModel)
        {
            if (!viewModel.SendToAll && !viewModel.SelectedEmails.Any())
            {
                ModelState.AddModelError(nameof(AdminSubscribeViewModel.SelectedEmails), "Select at least one recipient.");
            }

            if (!ModelState.IsValid)
            {
                return View("Subscribe", await CreateSubscribeViewModelAsync(viewModel));
            }

            var recipients = viewModel.SendToAll
                ? await GetSubscribeRecipientsQuery().ToListAsync()
                : viewModel.SelectedEmails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            TempData["SubscribeMessage"] = $"Message simulated for {recipients.Count} recipient(s).";

            return RedirectToAction(nameof(Subscribe));
        }

        private async Task<AdminSubscribeViewModel> CreateSubscribeViewModelAsync(AdminSubscribeViewModel viewModel)
        {
            viewModel.Users = await _context.Users.OrderBy(user => user.Email).ToListAsync();
            viewModel.Subscribers = await _context.Subscribers.OrderBy(subscriber => subscriber.Email).ToListAsync();
            return viewModel;
        }

        private IQueryable<string> GetSubscribeRecipientsQuery()
        {
            var userEmails = _context.Users.Select(user => user.Email);
            var subscriberEmails = _context.Subscribers.Select(subscriber => subscriber.Email);

            return userEmails.Concat(subscriberEmails).Distinct();
        }
    }
}
