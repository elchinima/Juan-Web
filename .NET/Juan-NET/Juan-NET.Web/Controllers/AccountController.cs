using System.Security.Cryptography;
using Juan_NET.Domain.Entities;
using Juan_NET.Persistence.Context;
using Juan_NET.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Juan_NET.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel viewModel)
        {
            if (await _context.Users.AnyAsync(user => user.Email == viewModel.Email))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "This email is already registered.");
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(viewModel.Password, salt, 100000, HashAlgorithmName.SHA256, 32);

            var user = new User
            {
                FullName = viewModel.FullName,
                Email = viewModel.Email,
                PasswordSalt = Convert.ToBase64String(salt),
                PasswordHash = Convert.ToBase64String(hash),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            TempData["RegisterMessage"] = "Registration completed successfully.";

            return RedirectToAction(nameof(Register));
        }
    }
}
