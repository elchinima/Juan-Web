namespace Juan_NET.Web.Controllers
{
    [Authorize]
    [AdminPermission(AdminPermissionKeys.Support)]
    public class SupportController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ImageStorageService _imageStorage;

        public SupportController(AppDbContext context, ImageStorageService imageStorage)
        {
            _context = context;
            _imageStorage = imageStorage;
        }

        public async Task<IActionResult> Index()
        {
            var reports = await BuildReportsAsync();
            var weekStats = await BuildWeekStatsAsync();

            var viewModel = new SupportDashboardViewModel
            {
                DailyReportTarget = 400,
                DailyBonusReportTarget = 800,
                WorkHourTarget = 8,
                BonusWorkHourTarget = 12,
                TodayReports = reports.Count(item => item.CreatedAt.Date == DateTime.UtcNow.Date),
                WeekReports = weekStats.Sum(item => item.Reports),
                OpenReports = reports.Count(item => item.Status != "Resolved"),
                ResolvedReports = reports.Count(item => item.Status == "Resolved"),
                RecentReports = reports.Take(5).ToList(),
                WeekStats = weekStats
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Reports()
        {
            return View(await BuildReportsAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.SupportTickets
                .Include(item => item.User)
                .Include(item => item.Messages)
                .ThenInclude(message => message.SenderUser)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (ticket is null)
            {
                return RedirectToAction(nameof(Reports));
            }

            var viewModel = new SupportTicketDetailsViewModel
            {
                Id = ticket.Id,
                Code = ticket.Code,
                Customer = ticket.User.FullName,
                Subject = ticket.Subject,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                Messages = ticket.Messages
                    .OrderBy(message => message.CreatedAt)
                    .Select(message => new SupportMessageViewModel
                    {
                        SenderName = message.SenderUser.FullName,
                        IsOperator = message.IsOperator,
                        Text = message.Text,
                        ImageUrl = message.ImageUrl,
                        CreatedAt = message.CreatedAt
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(SupportMessageInput input)
        {
            if (!input.TicketId.HasValue)
            {
                return RedirectToAction(nameof(Reports));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var text = input.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text) && input.ImageFile is not { Length: > 0 })
            {
                return RedirectToAction(nameof(Details), new { id = input.TicketId.Value });
            }

            var ticket = await _context.SupportTickets.FindAsync(input.TicketId.Value);
            if (ticket is null)
            {
                return RedirectToAction(nameof(Reports));
            }

            string? imageUrl = null;
            if (input.ImageFile is { Length: > 0 })
            {
                imageUrl = await _imageStorage.SaveSupportAttachmentAsWebpAsync(input.ImageFile);
            }

            ticket.OperatorUserId = userId.Value;
            ticket.Status = "In Progress";
            ticket.UpdatedAt = DateTime.UtcNow;

            _context.SupportMessages.Add(new SupportMessage
            {
                SupportTicketId = ticket.Id,
                SenderUserId = userId.Value,
                IsOperator = true,
                Text = text,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        private async Task<IReadOnlyList<SupportReportViewModel>> BuildReportsAsync()
        {
            return await _context.SupportTickets
                .Include(ticket => ticket.User)
                .Include(ticket => ticket.OperatorUser)
                .Include(ticket => ticket.Messages)
                .OrderByDescending(ticket => ticket.UpdatedAt)
                .Select(ticket => new SupportReportViewModel
                {
                    Id = ticket.Id,
                    Code = ticket.Code,
                    Customer = ticket.User.FullName,
                    Subject = ticket.Subject,
                    Priority = ticket.Priority,
                    Status = ticket.Status,
                    Operator = ticket.OperatorUser == null ? "Unassigned" : ticket.OperatorUser.FullName,
                    MessageCount = ticket.Messages.Count,
                    CreatedAt = ticket.CreatedAt
                })
                .ToListAsync();
        }

        private async Task<IReadOnlyList<SupportDayStatViewModel>> BuildWeekStatsAsync()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-6);
            var tickets = await _context.SupportTickets
                .Where(ticket => ticket.CreatedAt >= startDate)
                .GroupBy(ticket => ticket.CreatedAt.Date)
                .Select(group => new { Date = group.Key, Count = group.Count() })
                .ToListAsync();

            return Enumerable.Range(0, 7)
                .Select(index =>
                {
                    var date = startDate.AddDays(index);
                    var count = tickets.FirstOrDefault(item => item.Date == date)?.Count ?? 0;
                    return new SupportDayStatViewModel
                    {
                        Day = date.ToString("ddd", CultureInfo.InvariantCulture),
                        Reports = count,
                        Hours = 0
                    };
                })
                .ToList();
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
