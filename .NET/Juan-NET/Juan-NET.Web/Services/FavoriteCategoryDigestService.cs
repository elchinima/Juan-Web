namespace Juan_NET.Web.Services
{
    public class FavoriteCategoryDigestService : BackgroundService
    {
        private static readonly TimeSpan BakuOffset = TimeSpan.FromHours(4);
        private static readonly TimeSpan DeliveryTime = TimeSpan.FromHours(12);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FavoriteCategoryDigestService> _logger;

        public FavoriteCategoryDigestService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<FavoriteCategoryDigestService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await SendDueDigestsAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Favorite category digest delivery failed.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(GetDelayUntilNextDelivery(), stoppingToken);
                    await SendDueDigestsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Favorite category digest delivery failed.");
                }
            }
        }

        private async Task SendDueDigestsAsync(CancellationToken stoppingToken)
        {
            var utcNow = DateTimeOffset.UtcNow;
            var bakuNow = utcNow.ToOffset(BakuOffset);

            if (bakuNow.TimeOfDay < DeliveryTime)
            {
                return;
            }

            var sentForDate = bakuNow.Date;
            var windowEnd = new DateTimeOffset(sentForDate.Add(DeliveryTime), BakuOffset).UtcDateTime;
            var windowStart = windowEnd.AddDays(-1);

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            var pendingCategories = await context.ProductCategories
                .Where(productCategory => productCategory.Product.IsActive &&
                    productCategory.Product.CreatedAt > windowStart &&
                    productCategory.Product.CreatedAt <= windowEnd &&
                    !context.FavoriteCategoryDigests.Any(digest =>
                        digest.CategoryId == productCategory.CategoryId &&
                        digest.SentForDate == sentForDate))
                .Select(productCategory => productCategory.Category)
                .Distinct()
                .OrderBy(category => category.Name)
                .ToListAsync(stoppingToken);

            foreach (var category in pendingCategories)
            {
                var products = await context.Products
                    .Include(product => product.ProductCategories)
                    .ThenInclude(productCategory => productCategory.Category)
                    .Where(product => product.IsActive &&
                        product.CreatedAt > windowStart &&
                        product.CreatedAt <= windowEnd &&
                        product.ProductCategories.Any(productCategory => productCategory.CategoryId == category.Id))
                    .OrderByDescending(product => product.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                var recipients = await (
                    from favorite in context.UserFavoriteCategories
                    join user in context.Users on favorite.UserId equals user.Id
                    join subscriber in context.Subscribers on user.Email equals subscriber.Email
                    where favorite.CategoryId == category.Id
                    select user.Email)
                    .Distinct()
                    .ToListAsync(stoppingToken);

                if (products.Any() && recipients.Any())
                {
                    var subject = $"New {category.Name} arrivals at Juan";
                    var body = BuildProductDigestEmail(category.Name, products, GetBaseUrl());

                    foreach (var recipient in recipients)
                    {
                        try
                        {
                            await emailService.SendAsync(recipient, subject, body);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Favorite category digest email failed for {Recipient}.", recipient);
                        }
                    }
                }

                context.FavoriteCategoryDigests.Add(new FavoriteCategoryDigest
                {
                    CategoryId = category.Id,
                    SentForDate = sentForDate,
                    CreatedAt = utcNow.UtcDateTime
                });
            }

            await context.SaveChangesAsync(stoppingToken);
        }

        private static TimeSpan GetDelayUntilNextDelivery()
        {
            var bakuNow = DateTimeOffset.UtcNow.ToOffset(BakuOffset);
            var nextDelivery = new DateTimeOffset(bakuNow.Date.Add(DeliveryTime), BakuOffset);

            if (bakuNow >= nextDelivery)
            {
                nextDelivery = nextDelivery.AddDays(1);
            }

            return nextDelivery.UtcDateTime - DateTime.UtcNow;
        }

        private string GetBaseUrl()
        {
            return (_configuration["Site:BaseUrl"] ?? _configuration["EmailSettings:BaseUrl"] ?? "http://localhost:5219").TrimEnd('/');
        }

        private static string BuildProductDigestEmail(string categoryName, List<Product> products, string baseUrl)
        {
            var encodedCategory = System.Net.WebUtility.HtmlEncode(categoryName);
            var cards = new System.Text.StringBuilder();

            foreach (var product in products)
            {
                var imageUrl = ToAbsoluteUrl(product.ImageUrl, baseUrl);
                var productUrl = $"{baseUrl}/Products?query={Uri.EscapeDataString(product.Name)}";
                cards.Append($$"""
                    <tr>
                        <td style="padding:14px 0;border-bottom:1px solid #eeeeee;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                                <tr>
                                    <td width="112" style="padding-right:16px;">
                                        <img src="{{System.Net.WebUtility.HtmlEncode(imageUrl)}}" alt="{{System.Net.WebUtility.HtmlEncode(product.Name)}}" width="112" height="112" style="display:block;width:112px;height:112px;object-fit:cover;border-radius:10px;background:#f6f2ea;" />
                                    </td>
                                    <td style="font-family:Arial,sans-serif;color:#222222;">
                                        <h3 style="margin:0 0 7px;font-size:18px;line-height:1.3;">{{System.Net.WebUtility.HtmlEncode(product.Name)}}</h3>
                                        <p style="margin:0 0 10px;color:#777777;font-size:14px;line-height:1.55;">{{System.Net.WebUtility.HtmlEncode(product.Description ?? "Freshly added to Juan.")}}</p>
                                        <strong style="display:block;margin-bottom:12px;color:#111111;font-size:17px;">${{product.Price.ToString("0.00", CultureInfo.InvariantCulture)}}</strong>
                                        <a href="{{productUrl}}" style="display:inline-block;padding:10px 14px;background:#e3a51e;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:700;font-size:12px;text-transform:uppercase;">View Product</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    """);
            }

            return $$"""
                <div style="margin:0;padding:0;background:#f5f1ea;">
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f5f1ea;padding:28px 12px;">
                        <tr>
                            <td align="center">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:680px;background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #eee3d1;">
                                    <tr>
                                        <td style="padding:30px;background:#222222;color:#ffffff;font-family:Arial,sans-serif;">
                                            <span style="display:inline-block;margin-bottom:10px;color:#e3a51e;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:.08em;">Favorite Category</span>
                                            <h1 style="margin:0 0 10px;font-size:30px;line-height:1.15;">New {{encodedCategory}} arrivals</h1>
                                            <p style="margin:0;color:#dddddd;font-size:15px;line-height:1.7;">Here are the latest products added to a category you love.</p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="padding:10px 28px 20px;">
                                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                                                {{cards}}
                                            </table>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </div>
                """;
        }

        private static string ToAbsoluteUrl(string? url, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return $"{baseUrl}/main%20assets/img/product/product-1.jpg";
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUrl))
            {
                return absoluteUrl.ToString();
            }

            return $"{baseUrl}/{url.TrimStart('/')}".Replace(" ", "%20");
        }
    }
}
