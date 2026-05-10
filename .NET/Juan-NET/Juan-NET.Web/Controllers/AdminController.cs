using Juan_NET.Domain.Entities;
using Juan_NET.Persistence.Context;
using Juan_NET.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Juan_NET.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ImageStorageService _imageStorage;
        private readonly EmailService _emailService;

        public AdminController(AppDbContext context, ImageStorageService imageStorage, EmailService emailService)
        {
            _context = context;
            _imageStorage = imageStorage;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ProductCount = await _context.Products.CountAsync();
            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.CategoryCount = await _context.Categories.CountAsync();
            ViewBag.SliderCount = await _context.Sliders.CountAsync();
            ViewBag.ContactMessageCount = await _context.ContactMessages.CountAsync();
            return View();
        }

        public async Task<IActionResult> Products(string? search)
        {
            var normalizedSearch = search?.Trim();
            var productsQuery = _context.Products
                .Include(product => product.ProductCategories)
                .ThenInclude(productCategory => productCategory.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                productsQuery = productsQuery.Where(product =>
                    product.Name.Contains(normalizedSearch) ||
                    product.CategoryName.Contains(normalizedSearch) ||
                    (product.Description != null && product.Description.Contains(normalizedSearch)));
            }

            var viewModel = new AdminProductsViewModel
            {
                Search = normalizedSearch,
                Products = await productsQuery
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

            viewModel.SelectedCategoryIds = (viewModel.SelectedCategoryIds ?? new List<int>()).Distinct().ToList();

            if (viewModel.Product is null)
            {
                ModelState.AddModelError(nameof(AdminProductsViewModel.Product), "Product details are required.");
            }

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

            var product = viewModel.Product!;
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
        public async Task<IActionResult> EditProduct(AdminProductsViewModel viewModel)
        {
            ModelState.Remove(nameof(AdminProductsViewModel.Products));
            ModelState.Remove(nameof(AdminProductsViewModel.Categories));
            ModelState.Remove($"{nameof(AdminProductsViewModel.Product)}.{nameof(Product.CategoryName)}");
            ModelState.Remove($"{nameof(AdminProductsViewModel.Product)}.{nameof(Product.ProductCategories)}");

            viewModel.SelectedCategoryIds = (viewModel.SelectedCategoryIds ?? new List<int>()).Distinct().ToList();

            if (viewModel.Product is null)
            {
                ModelState.AddModelError(nameof(AdminProductsViewModel.Product), "Product details are required.");
            }

            if (viewModel.SelectedCategoryIds.Count < 1 || viewModel.SelectedCategoryIds.Count > 3)
            {
                ModelState.AddModelError(nameof(AdminProductsViewModel.SelectedCategoryIds), "Select from 1 to 3 categories.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["OpenEditProductModal"] = true;
                await PopulateProductsViewModelAsync(viewModel);
                return View("Products", viewModel);
            }

            var productInput = viewModel.Product!;
            var product = await _context.Products
                .Include(item => item.ProductCategories)
                .FirstOrDefaultAsync(item => item.Id == productInput.Id);

            if (product is null)
            {
                TempData["ProductMessage"] = "Product was not found.";
                return RedirectToAction(nameof(Products));
            }

            var selectedCategories = await _context.Categories
                .Where(category => viewModel.SelectedCategoryIds.Contains(category.Id))
                .OrderBy(category => category.Name)
                .ToListAsync();

            if (selectedCategories.Count != viewModel.SelectedCategoryIds.Count)
            {
                ModelState.AddModelError(nameof(AdminProductsViewModel.SelectedCategoryIds), "Selected categories are invalid.");
                ViewData["OpenEditProductModal"] = true;
                await PopulateProductsViewModelAsync(viewModel);
                return View("Products", viewModel);
            }

            product.Name = productInput.Name;
            product.Price = productInput.Price;
            product.StockCount = productInput.StockCount;
            product.Description = productInput.Description;
            product.IsActive = productInput.IsActive;
            product.CategoryName = string.Join(", ", selectedCategories.Select(category => category.Name));
            string? oldImageToDelete = null;

            if (viewModel.ImageFile is { Length: > 0 })
            {
                oldImageToDelete = product.ImageUrl;
                product.ImageUrl = await _imageStorage.SaveAsWebpAsync(viewModel.ImageFile, "products");
            }
            else if (!string.IsNullOrWhiteSpace(productInput.ImageUrl))
            {
                product.ImageUrl = productInput.ImageUrl;
            }

            product.ProductCategories.Clear();

            foreach (var category in selectedCategories)
            {
                product.ProductCategories.Add(new ProductCategory { ProductId = product.Id, CategoryId = category.Id });
            }

            await _context.SaveChangesAsync();
            _imageStorage.DeleteUpload(oldImageToDelete);
            TempData["ProductMessage"] = "Product updated successfully.";

            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is not null)
            {
                var imageUrl = product.ImageUrl;
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                _imageStorage.DeleteUpload(imageUrl);
                TempData["ProductMessage"] = "Product deleted successfully.";
            }

            return RedirectToAction(nameof(Products));
        }

        public async Task<IActionResult> Categories(string? search)
        {
            var normalizedSearch = search?.Trim();
            var categoriesQuery = _context.Categories
                .Include(category => category.ProductCategories)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                categoriesQuery = categoriesQuery.Where(category => category.Name.Contains(normalizedSearch));
            }

            var viewModel = new AdminCategoriesViewModel
            {
                Search = normalizedSearch,
                Categories = await categoriesQuery
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

            var name = viewModel.Category?.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError($"{nameof(AdminCategoriesViewModel.Category)}.{nameof(Category.Name)}", "Category name is required.");
            }

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

            if (await _context.Sliders.CountAsync() >= 6)
            {
                ModelState.AddModelError(string.Empty, "Maximum slider count is 6.");
                ModelState.AddModelError(nameof(AdminSlidersViewModel.Slider), "Maximum slider count is 6.");
            }

            if (viewModel.Slider is null)
            {
                ModelState.AddModelError(nameof(AdminSlidersViewModel.Slider), "Slider details are required.");
            }

            if (!ModelState.IsValid)
            {
                viewModel.Sliders = await _context.Sliders.OrderBy(slider => slider.DisplayOrder).ToListAsync();
                return View("Sliders", viewModel);
            }

            var slider = viewModel.Slider!;

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

            if (viewModel.Slider is null)
            {
                return RedirectToAction(nameof(Sliders));
            }

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
            string? oldImageToDelete = null;

            if (viewModel.ImageFile is { Length: > 0 })
            {
                oldImageToDelete = slider.ImageUrl;
                slider.ImageUrl = await _imageStorage.SaveAsWebpAsync(viewModel.ImageFile, "sliders");
            }
            else if (!string.IsNullOrWhiteSpace(viewModel.Slider.ImageUrl))
            {
                slider.ImageUrl = viewModel.Slider.ImageUrl;
            }

            await _context.SaveChangesAsync();
            _imageStorage.DeleteUpload(oldImageToDelete);
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
                var imageUrl = slider.ImageUrl;
                _context.Sliders.Remove(slider);
                await _context.SaveChangesAsync();
                _imageStorage.DeleteUpload(imageUrl);
                TempData["SliderMessage"] = "Slider deleted successfully.";
            }

            return RedirectToAction(nameof(Sliders));
        }

        public async Task<IActionResult> ContactMessages(string? search)
        {
            var normalizedSearch = search?.Trim();
            var messagesQuery = _context.ContactMessages.AsQueryable();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                messagesQuery = messagesQuery.Where(message =>
                    message.Name.Contains(normalizedSearch) ||
                    message.Email.Contains(normalizedSearch) ||
                    message.Message.Contains(normalizedSearch));
            }

            ViewBag.ContactSearch = normalizedSearch ?? string.Empty;

            return View(await messagesQuery.OrderByDescending(message => message.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Users(string? search)
        {
            var usersQuery = _context.Users.AsQueryable();
            var normalizedSearch = search?.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                usersQuery = usersQuery.Where(user => user.FullName.Contains(normalizedSearch) || user.Email.Contains(normalizedSearch));
            }

            ViewBag.UserSearch = normalizedSearch ?? string.Empty;

            var users = await usersQuery.OrderByDescending(user => user.CreatedAt).ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Subscribe(string? userSearch)
        {
            return View(await CreateSubscribeViewModelAsync(new AdminSubscribeViewModel { UserSearch = userSearch }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSubscribe(AdminSubscribeViewModel viewModel)
        {
            viewModel.SelectedEmails ??= [];

            if (!viewModel.SendToAll && !viewModel.SelectedEmails.Any())
            {
                ModelState.AddModelError(nameof(AdminSubscribeViewModel.SelectedEmails), "Select at least one recipient.");
            }

            if (!ModelState.IsValid)
            {
                return View("Subscribe", await CreateSubscribeViewModelAsync(viewModel));
            }

            var recipients = viewModel.SendToAll
                ? (await GetSubscribeRecipientsQuery().ToListAsync()).Concat(viewModel.SelectedEmails).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                : viewModel.SelectedEmails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var body = WebUtility.HtmlEncode(viewModel.Message).Replace("\n", "<br />");

            foreach (var recipient in recipients)
            {
                await _emailService.SendAsync(recipient, viewModel.Subject, $"<p>{body}</p>");
            }

            TempData["SubscribeMessage"] = $"Message sent to {recipients.Count} recipient(s).";

            return RedirectToAction(nameof(Subscribe), new { userSearch = viewModel.UserSearch });
        }

        private async Task PopulateProductsViewModelAsync(AdminProductsViewModel viewModel)
        {
            viewModel.Products = await _context.Products
                .Include(product => product.ProductCategories)
                .ThenInclude(productCategory => productCategory.Category)
                .OrderByDescending(product => product.CreatedAt)
                .ToListAsync();
            viewModel.Categories = await _context.Categories.OrderBy(category => category.Name).ToListAsync();
        }

        private async Task<AdminSubscribeViewModel> CreateSubscribeViewModelAsync(AdminSubscribeViewModel viewModel)
        {
            var usersQuery = _context.Users.AsQueryable();
            var normalizedSearch = viewModel.UserSearch?.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                usersQuery = usersQuery.Where(user => user.FullName.Contains(normalizedSearch) || user.Email.Contains(normalizedSearch));
            }

            viewModel.UserSearch = normalizedSearch;
            viewModel.Users = await usersQuery.OrderBy(user => user.Email).Take(20).ToListAsync();
            viewModel.Subscribers = await _context.Subscribers.OrderBy(subscriber => subscriber.Email).ToListAsync();
            return viewModel;
        }

        private IQueryable<string> GetSubscribeRecipientsQuery()
        {
            return _context.Subscribers.Select(subscriber => subscriber.Email).Distinct();
        }
    }
}
