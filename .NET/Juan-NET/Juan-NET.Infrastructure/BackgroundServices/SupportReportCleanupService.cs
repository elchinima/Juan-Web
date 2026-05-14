namespace Juan_NET.Infrastructure.BackgroundServices
{
    public class SupportReportCleanupService : BackgroundService
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(12);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SupportReportCleanupService> _logger;

        public SupportReportCleanupService(IServiceScopeFactory scopeFactory, ILogger<SupportReportCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await DeleteExpiredReportsAsync(stoppingToken);

            using var timer = new PeriodicTimer(CleanupInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DeleteExpiredReportsAsync(stoppingToken);
            }
        }

        private async Task DeleteExpiredReportsAsync(CancellationToken stoppingToken)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var imageStorage = scope.ServiceProvider.GetRequiredService<ImageStorageService>();
                var deleteBefore = DateTime.UtcNow.AddDays(-7);

                var expiredReports = await context.SupportTickets
                    .Include(ticket => ticket.Messages)
                    .Where(ticket => ticket.Status == "Resolved" && ticket.ClosedAt != null && ticket.ClosedAt <= deleteBefore)
                    .ToListAsync(stoppingToken);

                if (!expiredReports.Any())
                {
                    return;
                }

                foreach (var imageUrl in expiredReports.SelectMany(ticket => ticket.Messages).Select(message => message.ImageUrl).Where(url => !string.IsNullOrWhiteSpace(url)).Distinct())
                {
                    try
                    {
                        imageStorage.DeleteUpload(imageUrl);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(exception, "Failed to delete support upload {ImageUrl}", imageUrl);
                    }
                }

                context.SupportTickets.RemoveRange(expiredReports);
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to delete expired support reports");
            }
        }
    }
}
