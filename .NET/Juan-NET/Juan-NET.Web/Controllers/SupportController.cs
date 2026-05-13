namespace Juan_NET.Web.Controllers
{
    [Authorize]
    [AdminPermission(AdminPermissionKeys.Support)]
    public class SupportController : Controller
    {
        private const int MaxShiftSeconds = 12 * 60 * 60;
        private static readonly TimeSpan SupportLocalOffset = TimeSpan.FromHours(4);

        private readonly AppDbContext _context;
        private readonly ImageStorageService _imageStorage;

        public SupportController(AppDbContext context, ImageStorageService imageStorage)
        {
            _context = context;
            _imageStorage = imageStorage;
        }

        public async Task<IActionResult> Index()
        {
            var reports = await BuildReportsAsync(ticket => true);
            var queueReports = reports.Where(item => item.Status != "Resolved").ToList();
            var weekStats = await BuildWeekStatsAsync();
            var activeReport = await GetActiveTicketAsync();
            var userId = GetCurrentUserId();
            var shift = userId.HasValue ? await UpdateShiftAsync(userId.Value, true) : (Seconds: 0, LimitReached: false);
            var monthlyRatings = userId.HasValue ? await BuildMonthlyRatingStatsAsync(userId.Value) : (Average: 5.0m, Count: 0);

            var viewModel = new SupportDashboardViewModel
            {
                DailyReportTarget = 400,
                DailyBonusReportTarget = 800,
                WorkHourTarget = 8,
                BonusWorkHourTarget = 12,
                TodayReports = reports.Count(item => item.CreatedAt.Date == DateTime.UtcNow.Date),
                WeekReports = weekStats.Sum(item => item.Reports),
                OpenReports = queueReports.Count,
                ResolvedReports = reports.Count(item => item.Status == "Resolved"),
                MonthlyRating = monthlyRatings.Average,
                MonthlyRatingCount = monthlyRatings.Count,
                ShiftSeconds = shift.Seconds,
                IsShiftLimitReached = shift.LimitReached,
                RecentReports = queueReports.Take(5).ToList(),
                WeekStats = weekStats,
                ActiveReportId = activeReport?.Id,
                ActiveReportCode = activeReport?.Code
            };

            return View(viewModel);
        }

        public async Task<IActionResult> ShiftStatus()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var shift = await UpdateShiftAsync(userId.Value, true);

            return Json(new
            {
                seconds = shift.Seconds,
                hours = Math.Round(shift.Seconds / 3600m, 2, MidpointRounding.AwayFromZero),
                isLimitReached = shift.LimitReached
            });
        }

        public async Task<IActionResult> Reports()
        {
            var activeReport = await GetActiveTicketAsync();
            ViewBag.ActiveReportId = activeReport?.Id;

            return View(await BuildReportsAsync(ticket => ticket.Status == "Open" && ticket.OperatorUserId == null));
        }

        public async Task<IActionResult> ReportQueue()
        {
            var activeReport = await GetActiveTicketAsync();
            var reports = await BuildReportsAsync(ticket => ticket.Status == "Open" && ticket.OperatorUserId == null);

            return Json(new
            {
                activeReportId = activeReport?.Id,
                reports = reports.Select(report => new
                {
                    id = report.Id,
                    code = report.Code,
                    customer = report.Customer,
                    category = report.Topic,
                    priority = report.Priority,
                    status = report.Status
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var updatedAt = DateTime.UtcNow;
            var updatedCount = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [SupportTickets]
                SET [OperatorUserId] = {userId.Value}, [Status] = N'In Progress', [UpdatedAt] = {updatedAt}
                WHERE [Id] = {id}
                    AND [Status] = N'Open'
                    AND [OperatorUserId] IS NULL
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [SupportTickets] WITH (UPDLOCK, HOLDLOCK)
                        WHERE [OperatorUserId] = {userId.Value}
                            AND [Status] = N'In Progress'
                    )
                """);

            if (updatedCount == 0)
            {
                var hasActiveReport = await _context.SupportTickets
                    .AnyAsync(ticket => ticket.OperatorUserId == userId.Value && ticket.Status == "In Progress");

                return hasActiveReport
                    ? RedirectToAction(nameof(ActiveReport))
                    : RedirectToAction(nameof(Reports));
            }

            return RedirectToAction(nameof(ActiveReport));
        }

        public async Task<IActionResult> ActiveReport()
        {
            var ticket = await GetActiveTicketWithMessagesAsync();
            if (ticket is null)
            {
                return RedirectToAction(nameof(Reports));
            }

            return View(BuildTicketDetailsViewModel(ticket));
        }

        public async Task<IActionResult> ActiveReportMessages()
        {
            var ticket = await GetActiveTicketWithMessagesAsync();
            if (ticket is null)
            {
                return Json(new { isActive = false });
            }

            var viewModel = BuildTicketDetailsViewModel(ticket);

            return Json(new
            {
                isActive = true,
                status = viewModel.Status,
                messages = viewModel.Messages.Select(message => new
                {
                    senderName = message.SenderName,
                    isOperator = message.IsOperator,
                    text = message.Text,
                    imageUrl = message.ImageUrl
                })
            });
        }

        public async Task<IActionResult> History()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var reports = await BuildReportsAsync(ticket => ticket.Status == "Resolved" && ticket.OperatorUserId == userId.Value);

            return View(reports);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var ticket = await _context.SupportTickets
                .Include(item => item.User)
                .Include(item => item.Rating)
                .Include(item => item.Messages)
                .ThenInclude(message => message.SenderUser)
                .FirstOrDefaultAsync(item => item.Id == id && item.Status == "Resolved" && item.OperatorUserId == userId.Value);

            if (ticket is null)
            {
                return RedirectToAction(nameof(History));
            }

            return View(BuildTicketDetailsViewModel(ticket));
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
                return RedirectToAction(nameof(ActiveReport));
            }

            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(item => item.Id == input.TicketId.Value && item.OperatorUserId == userId.Value && item.Status == "In Progress");

            if (ticket is null)
            {
                return RedirectToAction(nameof(ActiveReport));
            }

            string? imageUrl = null;
            if (input.ImageFile is { Length: > 0 })
            {
                imageUrl = await _imageStorage.SaveSupportAttachmentAsWebpAsync(input.ImageFile);
            }

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

            return RedirectToAction(nameof(ActiveReport));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(item => item.Id == id && item.OperatorUserId == userId.Value && item.Status == "In Progress");

            if (ticket is null)
            {
                return RedirectToAction(nameof(ActiveReport));
            }

            ticket.Status = "Resolved";
            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.ClosedAt = ticket.UpdatedAt;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(History));
        }

        private async Task<IReadOnlyList<SupportReportViewModel>> BuildReportsAsync(Expression<Func<SupportTicket, bool>> predicate)
        {
            return await _context.SupportTickets
                .Where(predicate)
                .Include(ticket => ticket.User)
                .Include(ticket => ticket.OperatorUser)
                .Include(ticket => ticket.Messages)
                .Include(ticket => ticket.Rating)
                .OrderByDescending(ticket => ticket.UpdatedAt)
                .Select(ticket => new SupportReportViewModel
                {
                    Id = ticket.Id,
                    Code = ticket.Code,
                    Customer = ticket.User.FullName,
                    Topic = ticket.Topic,
                    Subject = ticket.Subject,
                    Priority = ticket.Priority,
                    Status = ticket.Status,
                    Operator = ticket.OperatorUser == null ? "Unassigned" : ticket.OperatorUser.FullName,
                    MessageCount = ticket.Messages.Count,
                    Rating = ticket.Rating == null ? null : ticket.Rating.Rating,
                    RatingComment = ticket.Rating == null ? null : ticket.Rating.Comment,
                    CreatedAt = ticket.CreatedAt,
                    UpdatedAt = ticket.UpdatedAt
                })
                .ToListAsync();
        }

        private async Task<SupportTicket?> GetActiveTicketAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            return await _context.SupportTickets
                .FirstOrDefaultAsync(ticket => ticket.OperatorUserId == userId.Value && ticket.Status == "In Progress");
        }

        private async Task<SupportTicket?> GetActiveTicketWithMessagesAsync()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return null;
            }

            return await _context.SupportTickets
                .Include(item => item.User)
                .Include(item => item.Rating)
                .Include(item => item.Messages)
                .ThenInclude(message => message.SenderUser)
                .FirstOrDefaultAsync(item => item.OperatorUserId == userId.Value && item.Status == "In Progress");
        }

        private static SupportTicketDetailsViewModel BuildTicketDetailsViewModel(SupportTicket ticket)
        {
            return new SupportTicketDetailsViewModel
            {
                Id = ticket.Id,
                Code = ticket.Code,
                Customer = ticket.User.FullName,
                Topic = ticket.Topic,
                Subject = ticket.Subject,
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                Rating = ticket.Rating == null ? null : ticket.Rating.Rating,
                RatingComment = ticket.Rating == null ? null : ticket.Rating.Comment,
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

        private async Task<(decimal Average, int Count)> BuildMonthlyRatingStatsAsync(int userId)
        {
            var (monthStartUtc, nextMonthStartUtc) = GetCurrentSupportMonthUtcRange();
            var ratings = await _context.SupportRatings
                .Where(rating => rating.OperatorUserId == userId && rating.CreatedAt >= monthStartUtc && rating.CreatedAt < nextMonthStartUtc)
                .Select(rating => rating.Rating)
                .ToListAsync();

            if (!ratings.Any())
            {
                return (5.0m, 0);
            }

            return (Math.Round(ratings.Average(), 1, MidpointRounding.AwayFromZero), ratings.Count);
        }

        private async Task<(int Seconds, bool LimitReached)> UpdateShiftAsync(int userId, bool startIfNotRunning)
        {
            var now = DateTime.UtcNow;
            var workDate = now.Add(SupportLocalOffset).Date;
            var shift = await _context.SupportOperatorWorkTimes
                .FirstOrDefaultAsync(item => item.OperatorUserId == userId && item.WorkDate == workDate);

            if (shift is null)
            {
                shift = new SupportOperatorWorkTime
                {
                    OperatorUserId = userId,
                    WorkDate = workDate,
                    LastStartedAt = startIfNotRunning ? now : null,
                    UpdatedAt = now
                };
                _context.SupportOperatorWorkTimes.Add(shift);
            }
            else if (shift.LastStartedAt.HasValue)
            {
                var secondsToAdd = Math.Max(0, (int)Math.Floor((now - shift.LastStartedAt.Value).TotalSeconds));
                shift.TotalSeconds = Math.Min(MaxShiftSeconds, shift.TotalSeconds + secondsToAdd);
                shift.LastStartedAt = shift.TotalSeconds >= MaxShiftSeconds ? null : now;
                shift.UpdatedAt = now;
            }

            if (shift.TotalSeconds >= MaxShiftSeconds)
            {
                shift.TotalSeconds = MaxShiftSeconds;
                shift.LastStartedAt = null;
            }
            else if (startIfNotRunning && !shift.LastStartedAt.HasValue)
            {
                shift.LastStartedAt = now;
                shift.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            return (shift.TotalSeconds, shift.TotalSeconds >= MaxShiftSeconds);
        }

        private static (DateTime MonthStartUtc, DateTime NextMonthStartUtc) GetCurrentSupportMonthUtcRange()
        {
            var supportNow = DateTime.UtcNow.Add(SupportLocalOffset);
            var monthStartLocal = new DateTime(supportNow.Year, supportNow.Month, 1);
            return (monthStartLocal.Subtract(SupportLocalOffset), monthStartLocal.AddMonths(1).Subtract(SupportLocalOffset));
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
