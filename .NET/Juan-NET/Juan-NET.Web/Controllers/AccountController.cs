using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Juan_NET.Domain.Entities;

namespace Juan_NET.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ImageStorageService _imageStorage;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly AdminAccessService _adminAccessService;

        public AccountController(AppDbContext context, ImageStorageService imageStorage, EmailService emailService, IConfiguration configuration, AdminAccessService adminAccessService)
        {
            _context = context;
            _imageStorage = imageStorage;
            _emailService = emailService;
            _configuration = configuration;
            _adminAccessService = adminAccessService;
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

            var (hash, salt) = HashSecret(viewModel.Password);
            var user = new User
            {
                FullName = viewModel.FullName.Trim(),
                Email = viewModel.Email.Trim().ToLowerInvariant(),
                PasswordSalt = salt,
                PasswordHash = hash,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            await _adminAccessService.EnsureDeveloperRoleAssignmentAsync(user);
            await SignInAsync(user, false);

            return RedirectToAction(nameof(Profile));
        }

        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var email = viewModel.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Email == email);

            if (user is null || !VerifySecret(viewModel.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
                return View(viewModel);
            }

            await _adminAccessService.EnsureDeveloperRoleAssignmentAsync(user);

            if (user.IsTwoFactorEnabled)
            {
                await SendTwoFactorCodeAsync(user);
                return RedirectToAction(nameof(TwoFactor), new { userId = user.Id, rememberMe = viewModel.RememberMe });
            }

            await SignInAsync(user, viewModel.RememberMe);
            return RedirectToAction(nameof(Profile));
        }

        public IActionResult GoogleLogin()
        {
            var google = _configuration.GetSection("Authentication:Google");
            var redirectUri = $"{Request.Scheme}://{Request.Host}/signin-google";
            var url = "https://accounts.google.com/o/oauth2/v2/auth"
                + $"?client_id={Uri.EscapeDataString(google["ClientId"] ?? string.Empty)}"
                + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
                + "&response_type=code"
                + $"&scope={Uri.EscapeDataString("openid email profile")}";

            return Redirect(url);
        }

        public IActionResult MetaLogin()
        {
            var meta = _configuration.GetSection("Authentication:Meta");
            var clientId = meta["ClientId"];

            if (string.IsNullOrWhiteSpace(clientId))
            {
                TempData["AuthMessage"] = "Meta sign in is not configured yet.";
                return RedirectToAction(nameof(Login));
            }

            var redirectUri = $"{Request.Scheme}://{Request.Host}/signin-meta";
            var url = "https://www.facebook.com/v20.0/dialog/oauth"
                + $"?client_id={Uri.EscapeDataString(clientId)}"
                + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
                + "&response_type=code"
                + $"&scope={Uri.EscapeDataString("email public_profile")}";

            return Redirect(url);
        }

        public IActionResult XLogin()
        {
            var x = _configuration.GetSection("Authentication:X");

            if (string.IsNullOrWhiteSpace(x["ClientId"]))
            {
                TempData["AuthMessage"] = "X sign in is not configured yet.";
                return RedirectToAction(nameof(Login));
            }

            TempData["AuthMessage"] = "X sign in needs OAuth 2.0 PKCE setup before it can be used.";
            return RedirectToAction(nameof(Login));
        }

        [Route("signin-google")]
        public async Task<IActionResult> GoogleCallback(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["AuthMessage"] = "Google sign in was cancelled.";
                return RedirectToAction(nameof(Login));
            }

            var google = _configuration.GetSection("Authentication:Google");
            var redirectUri = $"{Request.Scheme}://{Request.Host}/signin-google";

            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = google["ClientId"] ?? string.Empty,
                ["client_secret"] = google["ClientSecret"] ?? string.Empty,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            }));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                TempData["AuthMessage"] = "Google sign in failed.";
                return RedirectToAction(nameof(Login));
            }

            using var tokenJson = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync());
            var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();

            using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var userResponse = await httpClient.SendAsync(userRequest);

            if (!userResponse.IsSuccessStatusCode)
            {
                TempData["AuthMessage"] = "Google profile could not be loaded.";
                return RedirectToAction(nameof(Login));
            }

            using var profileJson = await JsonDocument.ParseAsync(await userResponse.Content.ReadAsStreamAsync());
            var email = profileJson.RootElement.GetProperty("email").GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
            var fullName = profileJson.RootElement.TryGetProperty("name", out var name) ? name.GetString() ?? email : email;
            var providerId = profileJson.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() : null;

            var user = await _context.Users.FirstOrDefaultAsync(item => item.Email == email);

            if (user is null)
            {
                var (hash, salt) = HashSecret(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
                user = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    ExternalProvider = "Google",
                    ExternalProviderId = providerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
            }
            else
            {
                user.ExternalProvider = "Google";
                user.ExternalProviderId = providerId;
            }

            await _context.SaveChangesAsync();
            await _adminAccessService.EnsureDeveloperRoleAssignmentAsync(user);
            await SignInAsync(user, true);
            return RedirectToAction(nameof(Profile));
        }

        [Route("signin-meta")]
        public async Task<IActionResult> MetaCallback(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["AuthMessage"] = "Meta sign in was cancelled.";
                return RedirectToAction(nameof(Login));
            }

            var meta = _configuration.GetSection("Authentication:Meta");
            var redirectUri = $"{Request.Scheme}://{Request.Host}/signin-meta";

            using var httpClient = new HttpClient();
            var tokenUrl = "https://graph.facebook.com/v20.0/oauth/access_token"
                + $"?client_id={Uri.EscapeDataString(meta["ClientId"] ?? string.Empty)}"
                + $"&client_secret={Uri.EscapeDataString(meta["ClientSecret"] ?? string.Empty)}"
                + $"&code={Uri.EscapeDataString(code)}"
                + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}";
            using var tokenResponse = await httpClient.GetAsync(tokenUrl);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                TempData["AuthMessage"] = "Meta sign in failed.";
                return RedirectToAction(nameof(Login));
            }

            using var tokenJson = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync());
            var accessToken = tokenJson.RootElement.GetProperty("access_token").GetString();
            var profileUrl = "https://graph.facebook.com/me"
                + $"?fields={Uri.EscapeDataString("id,name,email")}"
                + $"&access_token={Uri.EscapeDataString(accessToken ?? string.Empty)}";
            using var profileResponse = await httpClient.GetAsync(profileUrl);

            if (!profileResponse.IsSuccessStatusCode)
            {
                TempData["AuthMessage"] = "Meta profile could not be loaded.";
                return RedirectToAction(nameof(Login));
            }

            using var profileJson = await JsonDocument.ParseAsync(await profileResponse.Content.ReadAsStreamAsync());
            var email = profileJson.RootElement.TryGetProperty("email", out var emailElement)
                ? emailElement.GetString()?.Trim().ToLowerInvariant()
                : null;

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["AuthMessage"] = "Meta account did not provide an email address.";
                return RedirectToAction(nameof(Login));
            }

            var fullName = profileJson.RootElement.TryGetProperty("name", out var name) ? name.GetString() ?? email : email;
            var providerId = profileJson.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Email == email);

            if (user is null)
            {
                var (hash, salt) = HashSecret(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
                user = new User
                {
                    FullName = fullName,
                    Email = email,
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    ExternalProvider = "Meta",
                    ExternalProviderId = providerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(user);
            }
            else
            {
                user.ExternalProvider = "Meta";
                user.ExternalProviderId = providerId;
            }

            await _context.SaveChangesAsync();
            await _adminAccessService.EnsureDeveloperRoleAssignmentAsync(user);
            await SignInAsync(user, true);
            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> TwoFactor(int userId, bool rememberMe)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user is null)
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new TwoFactorViewModel { UserId = userId, RememberMe = rememberMe });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactor(TwoFactorViewModel viewModel)
        {
            var user = await _context.Users.FindAsync(viewModel.UserId);

            if (user is null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid || user.TwoFactorCodeExpiresAt < DateTime.UtcNow || !VerifySecret(viewModel.Code, user.TwoFactorCodeHash, user.TwoFactorCodeSalt))
            {
                ModelState.AddModelError(nameof(TwoFactorViewModel.Code), "The verification code is invalid or expired.");
                return View(viewModel);
            }

            user.TwoFactorCodeHash = null;
            user.TwoFactorCodeSalt = null;
            user.TwoFactorCodeExpiresAt = null;
            await _context.SaveChangesAsync();
            await SignInAsync(user, viewModel.RememberMe);

            return RedirectToAction(nameof(Profile));
        }

        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var email = viewModel.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Email == email);

            if (user is not null)
            {
                var token = CreateUrlToken();
                var (hash, salt) = HashSecret(token);
                user.PasswordResetTokenHash = hash;
                user.PasswordResetTokenSalt = salt;
                user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                var link = Url.Action(nameof(ResetPassword), "Account", new { email = user.Email, token }, Request.Scheme);
                await _emailService.SendAsync(user.Email, "Juan password reset", $"<p>Use this link to reset your password:</p><p><a href=\"{link}\">Reset password</a></p>");
            }

            TempData["AuthMessage"] = "If this email exists, a reset link has been sent.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        public IActionResult ResetPassword(string email, string token)
        {
            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var email = viewModel.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Email == email);

            if (user is null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow || !VerifySecret(viewModel.Token, user.PasswordResetTokenHash, user.PasswordResetTokenSalt))
            {
                ModelState.AddModelError(string.Empty, "Password reset link is invalid or expired.");
                return View(viewModel);
            }

            var (hash, salt) = HashSecret(viewModel.Password);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenSalt = null;
            user.PasswordResetTokenExpiresAt = null;
            await _context.SaveChangesAsync();

            TempData["AuthMessage"] = "Password changed. You can sign in now.";
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentUserAsync();
            return user is null ? RedirectToAction(nameof(Login)) : View(CreateProfileViewModel(user));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel viewModel)
        {
            ModelState.Remove(nameof(ProfileViewModel.Users));
            ModelState.Remove(nameof(ProfileViewModel.ImageFile));
            ModelState.Remove(nameof(ProfileViewModel.Email));
            ModelState.Remove(nameof(ProfileViewModel.ChangePassword));

            var user = await GetCurrentUserAsync();

            if (user is null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", CreateProfileViewModel(user));
            }

            user.FullName = viewModel.FullName.Trim();
            await _context.SaveChangesAsync();
            await SignInAsync(user, true);
            TempData["ProfileMessage"] = "Profile updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTwoFactor(bool isTwoFactorEnabled)
        {
            var user = await GetCurrentUserAsync();

            if (user is null)
            {
                return RedirectToAction(nameof(Login));
            }

            user.IsTwoFactorEnabled = isTwoFactorEnabled;
            await _context.SaveChangesAsync();
            TempData["ProfileMessage"] = user.IsTwoFactorEnabled ? "Two-factor authentication enabled." : "Two-factor authentication disabled.";

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPasswordChange(ProfileViewModel viewModel)
        {
            var user = await GetCurrentUserAsync();

            if (user is null)
            {
                return RedirectToAction(nameof(Login));
            }

            ModelState.Clear();
            TryValidateModel(viewModel.ChangePassword, nameof(ProfileViewModel.ChangePassword));

            if (!ModelState.IsValid || !VerifySecret(viewModel.ChangePassword.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError($"{nameof(ProfileViewModel.ChangePassword)}.{nameof(ChangePasswordViewModel.CurrentPassword)}", "Current password is incorrect.");
                return View("Profile", CreateProfileViewModel(user, viewModel.ChangePassword));
            }

            var (passwordHash, passwordSalt) = HashSecret(viewModel.ChangePassword.NewPassword);
            var token = CreateUrlToken();
            var (tokenHash, tokenSalt) = HashSecret(token);
            user.PendingPasswordHash = passwordHash;
            user.PendingPasswordSalt = passwordSalt;
            user.PasswordChangeTokenHash = tokenHash;
            user.PasswordChangeTokenSalt = tokenSalt;
            user.PasswordChangeTokenExpiresAt = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            var link = Url.Action(nameof(ConfirmPasswordChange), "Account", new { email = user.Email, token }, Request.Scheme);
            await _emailService.SendAsync(user.Email, "Juan password change confirmation", $"<p>Confirm your password change here:</p><p><a href=\"{link}\">Confirm password change</a></p>");
            TempData["ProfileMessage"] = "A confirmation link has been sent to your email.";

            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> ConfirmPasswordChange(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["AuthMessage"] = "Password change link is invalid or expired.";
                return RedirectToAction(nameof(Login));
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(item => item.Email == normalizedEmail);

            if (user is null)
            {
                TempData["AuthMessage"] = "Password change link is invalid or expired.";
                return RedirectToAction(nameof(Login));
            }

            if (user.PasswordChangeTokenExpiresAt < DateTime.UtcNow || !VerifySecret(token, user.PasswordChangeTokenHash, user.PasswordChangeTokenSalt) || user.PendingPasswordHash is null || user.PendingPasswordSalt is null)
            {
                TempData["AuthMessage"] = "Password change link is invalid or expired.";
                return RedirectToAction(nameof(Login));
            }

            user.PasswordHash = user.PendingPasswordHash;
            user.PasswordSalt = user.PendingPasswordSalt;
            user.PendingPasswordHash = null;
            user.PendingPasswordSalt = null;
            user.PasswordChangeTokenHash = null;
            user.PasswordChangeTokenSalt = null;
            user.PasswordChangeTokenExpiresAt = null;
            await _context.SaveChangesAsync();
            TempData["AuthMessage"] = "Password changed successfully.";

            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfileImage(ProfileViewModel viewModel)
        {
            var user = await GetCurrentUserAsync();

            if (user is not null && viewModel.ImageFile is { Length: > 0 })
            {
                user.ProfileImageUrl = await _imageStorage.SaveAsWebpAsync(viewModel.ImageFile, "profiles");
                await _context.SaveChangesAsync();
                TempData["ProfileMessage"] = "Profile image saved successfully.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var user = await GetCurrentUserAsync();

            if (user is not null)
            {
                user.ProfileImageUrl = null;
                await _context.SaveChangesAsync();
                TempData["ProfileMessage"] = "Profile image removed successfully.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out var id) ? await _context.Users.FindAsync(id) : null;
        }

        private async Task SignInAsync(User user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
            });
        }

        private async Task SendTwoFactorCodeAsync(User user)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            var (hash, salt) = HashSecret(code);
            user.TwoFactorCodeHash = hash;
            user.TwoFactorCodeSalt = salt;
            user.TwoFactorCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);
            await _context.SaveChangesAsync();
            await _emailService.SendAsync(user.Email, "Juan verification code", $"<p>Your Juan login code is <strong>{code}</strong>.</p><p>It expires in 10 minutes.</p>");
        }

        private static ProfileViewModel CreateProfileViewModel(User user, ChangePasswordViewModel? changePassword = null)
        {
            return new ProfileViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                ProfileImageUrl = user.ProfileImageUrl,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                ChangePassword = changePassword ?? new ChangePasswordViewModel()
            };
        }

        private static (string Hash, string Salt) HashSecret(string value)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(value, salt, 100000, HashAlgorithmName.SHA256, 32);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        private static bool VerifySecret(string? value, string? hash, string? salt)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt))
            {
                return false;
            }

            var saltBytes = Convert.FromBase64String(salt);
            var testHash = Rfc2898DeriveBytes.Pbkdf2(value, saltBytes, 100000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(testHash, Convert.FromBase64String(hash));
        }

        private static string CreateUrlToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
