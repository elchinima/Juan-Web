using Juan_NET.Domain.Entities;
using Juan_NET.Persistence.Context;
using Juan_NET.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Juan_NET.Web.Controllers
{
    [Authorize]
    [AdminPermission(AdminPermissionKeys.AdminAccess)]
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
            ViewBag.RoleCount = await _context.AdminRoles.CountAsync();
            return View();
        }

        [AdminPermission(AdminPermissionKeys.Products)]
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
        [AdminPermission(AdminPermissionKeys.Products)]
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
        [AdminPermission(AdminPermissionKeys.Products)]
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
        [AdminPermission(AdminPermissionKeys.Products)]
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

        [AdminPermission(AdminPermissionKeys.Categories)]
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
        [AdminPermission(AdminPermissionKeys.Categories)]
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
        [AdminPermission(AdminPermissionKeys.Categories)]
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

        [AdminPermission(AdminPermissionKeys.Sliders)]
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
        [AdminPermission(AdminPermissionKeys.Sliders)]
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
        [AdminPermission(AdminPermissionKeys.Sliders)]
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
        [AdminPermission(AdminPermissionKeys.Sliders)]
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

        [AdminPermission(AdminPermissionKeys.ContactMessages)]
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

        [AdminPermission(AdminPermissionKeys.Users)]
        public async Task<IActionResult> Users(string? search)
        {
            var usersQuery = _context.Users
                .Include(user => user.AdminRoles)
                .ThenInclude(userRole => userRole.AdminRole)
                .AsQueryable();
            var normalizedSearch = search?.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                usersQuery = usersQuery.Where(user => user.FullName.Contains(normalizedSearch) || user.Email.Contains(normalizedSearch));
            }

            var viewModel = new AdminUsersViewModel
            {
                Search = normalizedSearch ?? string.Empty,
                Users = await usersQuery.OrderByDescending(user => user.CreatedAt).ToListAsync()
            };

            return View(viewModel);
        }

        [AdminPermission(AdminPermissionKeys.Subscribe)]
        public async Task<IActionResult> Subscribe(string? userSearch)
        {
            return View(await CreateSubscribeViewModelAsync(new AdminSubscribeViewModel { UserSearch = userSearch }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminPermission(AdminPermissionKeys.Subscribe)]
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

        [AdminPermission(AdminPermissionKeys.Roles)]
        public async Task<IActionResult> Roles(int? editId)
        {
            return View(await CreateRolesViewModelAsync(editId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminPermission(AdminPermissionKeys.Roles)]
        public async Task<IActionResult> AddRole(AdminRolesViewModel viewModel)
        {
            PrepareRoleModelState();
            NormalizeRoleInput(viewModel);
            var currentUserHighestRoleOrder = await GetCurrentUserHighestRoleOrderAsync();

            if (await _context.AdminRoles.AnyAsync(role => role.Name == viewModel.Role.Name))
            {
                ModelState.AddModelError(nameof(AdminRolesViewModel.Role) + "." + nameof(AdminRoleFormViewModel.Name), "Role already exists.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateRolesViewModelAsync(viewModel);
                return View("Roles", viewModel);
            }

            var role = new AdminRole
            {
                Name = viewModel.Role.Name,
                Color = viewModel.Role.Color,
                DisplayOrder = await GetNextRoleDisplayOrderAsync()
            };

            ApplyRolePermissions(role, viewModel.SelectedPermissionKeys);
            await ApplyRoleUsersAsync(role, viewModel.SelectedUserIds, currentUserHighestRoleOrder);
            _context.AdminRoles.Add(role);
            await _context.SaveChangesAsync();
            TempData["RoleMessage"] = "Role added successfully.";

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminPermission(AdminPermissionKeys.Roles)]
        public async Task<IActionResult> EditRole(AdminRolesViewModel viewModel)
        {
            PrepareRoleModelState();
            NormalizeRoleInput(viewModel);
            var currentUserHighestRoleOrder = await GetCurrentUserHighestRoleOrderAsync();

            var role = await _context.AdminRoles
                .Include(item => item.Permissions)
                .Include(item => item.UserRoles)
                .FirstOrDefaultAsync(item => item.Id == viewModel.Role.Id);

            if (role is null)
            {
                TempData["RoleMessage"] = "Role was not found.";
                return RedirectToAction(nameof(Roles));
            }

            if (!CanManageRole(role, currentUserHighestRoleOrder))
            {
                TempData["RoleMessage"] = "You cannot edit this role.";
                return RedirectToAction(nameof(Roles));
            }

            if (await _context.AdminRoles.AnyAsync(item => item.Id != role.Id && item.Name == viewModel.Role.Name))
            {
                ModelState.AddModelError(nameof(AdminRolesViewModel.Role) + "." + nameof(AdminRoleFormViewModel.Name), "Role already exists.");
            }

            if (!ModelState.IsValid)
            {
                viewModel.EditingRoleId = role.Id;
                await PopulateRolesViewModelAsync(viewModel);
                return View("Roles", viewModel);
            }

            role.Name = viewModel.Role.Name;
            role.Color = viewModel.Role.Color;
            role.Permissions.Clear();
            ApplyRolePermissions(role, viewModel.SelectedPermissionKeys);

            await _context.SaveChangesAsync();
            TempData["RoleMessage"] = "Role updated successfully.";

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminPermission(AdminPermissionKeys.Roles)]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.AdminRoles.FindAsync(id);
            var currentUserHighestRoleOrder = await GetCurrentUserHighestRoleOrderAsync();

            if (role is not null)
            {
                if (!CanManageRole(role, currentUserHighestRoleOrder))
                {
                    TempData["RoleMessage"] = "You cannot delete this role.";
                    return RedirectToAction(nameof(Roles));
                }

                _context.AdminRoles.Remove(role);
                await _context.SaveChangesAsync();
                TempData["RoleMessage"] = "Role deleted successfully.";
            }

            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminPermission(AdminPermissionKeys.Roles)]
        public async Task<IActionResult> ReorderRoles(List<int> roleIds)
        {
            roleIds ??= [];
            var currentUserHighestRoleOrder = await GetCurrentUserHighestRoleOrderAsync();
            var roles = await _context.AdminRoles.ToListAsync();

            if (roleIds.Count != roles.Count || roles.Any(role => !roleIds.Contains(role.Id)))
            {
                TempData["RoleMessage"] = "Role order was not saved.";
                return RedirectToAction(nameof(Roles));
            }

            var orderByRoleId = roleIds.Select((roleId, index) => new { roleId, index }).ToDictionary(item => item.roleId, item => item.index);

            foreach (var role in roles)
            {
                if (!CanManageRole(role, currentUserHighestRoleOrder) && orderByRoleId[role.Id] != role.DisplayOrder)
                {
                    TempData["RoleMessage"] = "You cannot move this role.";
                    return RedirectToAction(nameof(Roles));
                }

                if (orderByRoleId[role.Id] <= currentUserHighestRoleOrder && role.DisplayOrder > currentUserHighestRoleOrder)
                {
                    TempData["RoleMessage"] = "You cannot move roles to your level or higher.";
                    return RedirectToAction(nameof(Roles));
                }
            }

            foreach (var role in roles)
            {
                role.DisplayOrder = orderByRoleId[role.Id];
            }

            await _context.SaveChangesAsync();
            TempData["RoleMessage"] = "Role order updated successfully.";
            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminPermission(AdminPermissionKeys.Roles)]
        public async Task<IActionResult> AssignRoleToUsers(AdminRolesViewModel viewModel)
        {
            viewModel.SelectedUserIds ??= [];
            var currentUserHighestRoleOrder = await GetCurrentUserHighestRoleOrderAsync();
            var role = await _context.AdminRoles
                .Include(item => item.UserRoles)
                .FirstOrDefaultAsync(item => item.Id == viewModel.AssignRoleId);

            if (role is null || !CanManageRole(role, currentUserHighestRoleOrder))
            {
                TempData["RoleMessage"] = "You cannot assign this role.";
                return RedirectToAction(nameof(Roles));
            }

            var selectedUserIds = await _context.Users
                .Where(user => viewModel.SelectedUserIds.Contains(user.Id))
                .Select(user => user.Id)
                .ToListAsync();
            var existingUserIds = role.UserRoles.Select(userRole => userRole.UserId).ToHashSet();

            foreach (var userId in selectedUserIds.Where(userId => !existingUserIds.Contains(userId)))
            {
                role.UserRoles.Add(new UserAdminRole { UserId = userId, AdminRoleId = role.Id });
            }

            await _context.SaveChangesAsync();
            TempData["RoleMessage"] = "Role assigned successfully.";
            return RedirectToAction(nameof(Roles));
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

        private async Task<AdminRolesViewModel> CreateRolesViewModelAsync(int? editId)
        {
            var viewModel = new AdminRolesViewModel();
            await PopulateRolesViewModelAsync(viewModel);

            if (editId is null)
            {
                return viewModel;
            }

            var role = viewModel.Roles.FirstOrDefault(item => item.Id == editId.Value);

            if (role is null)
            {
                return viewModel;
            }

            if (!CanManageRole(role, viewModel.CurrentUserHighestRoleOrder))
            {
                return viewModel;
            }

            viewModel.EditingRoleId = role.Id;
            viewModel.Role = new AdminRoleFormViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Color = role.Color
            };
            viewModel.SelectedPermissionKeys = role.Permissions.Select(permission => permission.PermissionKey).ToList();
            viewModel.SelectedUserIds = role.UserRoles.Select(userRole => userRole.UserId).ToList();
            return viewModel;
        }

        private async Task PopulateRolesViewModelAsync(AdminRolesViewModel viewModel)
        {
            viewModel.Roles = await _context.AdminRoles
                .Include(role => role.Permissions)
                .Include(role => role.UserRoles)
                .ThenInclude(userRole => userRole.User)
                .OrderBy(role => role.DisplayOrder)
                .ThenBy(role => role.Name)
                .ToListAsync();
            viewModel.Users = await _context.Users
                .Include(user => user.AdminRoles)
                .ThenInclude(userRole => userRole.AdminRole)
                .OrderBy(user => user.Email)
                .ToListAsync();
            viewModel.AvailablePermissions = AdminPermissionCatalog.Items.ToList();
            viewModel.CurrentUserHighestRoleOrder = await GetCurrentUserHighestRoleOrderAsync();
        }

        private void PrepareRoleModelState()
        {
            ModelState.Remove(nameof(AdminRolesViewModel.Roles));
            ModelState.Remove(nameof(AdminRolesViewModel.Users));
            ModelState.Remove(nameof(AdminRolesViewModel.AvailablePermissions));
        }

        private static void NormalizeRoleInput(AdminRolesViewModel viewModel)
        {
            viewModel.SelectedPermissionKeys ??= [];
            viewModel.SelectedUserIds ??= [];
            viewModel.Role.Name = (viewModel.Role.Name ?? string.Empty).Trim();
            viewModel.Role.Color = NormalizeRoleColor(viewModel.Role.Color);
            viewModel.SelectedPermissionKeys = viewModel.SelectedPermissionKeys
                .Where(AdminPermissionCatalog.AllKeys.Contains)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            viewModel.SelectedUserIds = viewModel.SelectedUserIds.Distinct().ToList();
        }

        private static string NormalizeRoleColor(string? color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return "#e3a51e";
            }

            color = color.Trim();
            return color.Length == 7 && color.StartsWith("#") ? color : "#e3a51e";
        }

        private static void ApplyRolePermissions(AdminRole role, IEnumerable<string> selectedPermissionKeys)
        {
            foreach (var permissionKey in selectedPermissionKeys)
            {
                role.Permissions.Add(new AdminRolePermission { PermissionKey = permissionKey });
            }
        }

        private async Task ApplyRoleUsersAsync(AdminRole role, IEnumerable<int> selectedUserIds, int currentUserHighestRoleOrder)
        {
            var userIds = await _context.Users
                .Where(user => selectedUserIds.Contains(user.Id))
                .Select(user => user.Id)
                .ToListAsync();

            foreach (var userId in CanManageRole(role, currentUserHighestRoleOrder) ? userIds : [])
            {
                role.UserRoles.Add(new UserAdminRole { UserId = userId });
            }
        }

        private async Task<int> GetCurrentUserHighestRoleOrderAsync()
        {
            var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(idValue, out var userId))
            {
                return int.MaxValue;
            }

            return await _context.UserAdminRoles
                .Where(userRole => userRole.UserId == userId)
                .Select(userRole => (int?)userRole.AdminRole.DisplayOrder)
                .MinAsync() ?? int.MaxValue;
        }

        private async Task<int> GetNextRoleDisplayOrderAsync()
        {
            return (await _context.AdminRoles.Select(role => (int?)role.DisplayOrder).MaxAsync() ?? -1) + 1;
        }

        private static bool CanManageRole(AdminRole role, int currentUserHighestRoleOrder)
        {
            return role.DisplayOrder > currentUserHighestRoleOrder;
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
