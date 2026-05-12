namespace Juan_NET.Web.Controllers
{
    [Authorize]
    [AdminPermission(AdminPermissionKeys.Support)]
    public class SupportController : Controller
    {
        public IActionResult Index()
        {
            var reports = BuildReports();
            var weekStats = BuildWeekStats();

            var viewModel = new SupportDashboardViewModel
            {
                DailyReportTarget = 400,
                DailyBonusReportTarget = 800,
                WorkHourTarget = 8,
                BonusWorkHourTarget = 12,
                TodayReports = 286,
                WeekReports = weekStats.Sum(item => item.Reports),
                OpenReports = reports.Count(item => item.Status != "Resolved"),
                ResolvedReports = reports.Count(item => item.Status == "Resolved"),
                RecentReports = reports.Take(5).ToList(),
                WeekStats = weekStats
            };

            return View(viewModel);
        }

        public IActionResult Reports()
        {
            return View(BuildReports());
        }

        private static IReadOnlyList<SupportReportViewModel> BuildReports()
        {
            var now = DateTime.Now;

            return
            [
                new SupportReportViewModel { Code = "SUP-1048", Customer = "Aydan M.", Subject = "Payment was captured twice", Priority = "High", Status = "Open", CreatedAt = now.AddMinutes(-7) },
                new SupportReportViewModel { Code = "SUP-1047", Customer = "Murad A.", Subject = "Order tracking page is empty", Priority = "Medium", Status = "In Progress", CreatedAt = now.AddMinutes(-18) },
                new SupportReportViewModel { Code = "SUP-1046", Customer = "Leyla S.", Subject = "Cannot change delivery address", Priority = "High", Status = "Open", CreatedAt = now.AddMinutes(-31) },
                new SupportReportViewModel { Code = "SUP-1045", Customer = "Nihat R.", Subject = "Product image is missing", Priority = "Low", Status = "Resolved", CreatedAt = now.AddMinutes(-52) },
                new SupportReportViewModel { Code = "SUP-1044", Customer = "Farida H.", Subject = "Refund confirmation not received", Priority = "Medium", Status = "In Progress", CreatedAt = now.AddHours(-1).AddMinutes(-16) },
                new SupportReportViewModel { Code = "SUP-1043", Customer = "Orkhan T.", Subject = "Promo code does not apply", Priority = "Low", Status = "Resolved", CreatedAt = now.AddHours(-2).AddMinutes(-4) },
                new SupportReportViewModel { Code = "SUP-1042", Customer = "Samira Q.", Subject = "Account email verification failed", Priority = "Medium", Status = "Open", CreatedAt = now.AddHours(-2).AddMinutes(-37) },
                new SupportReportViewModel { Code = "SUP-1041", Customer = "Elvin B.", Subject = "Wrong shoe size arrived", Priority = "High", Status = "In Progress", CreatedAt = now.AddHours(-3).AddMinutes(-11) }
            ];
        }

        private static IReadOnlyList<SupportDayStatViewModel> BuildWeekStats()
        {
            return
            [
                new SupportDayStatViewModel { Day = "Mon", Reports = 428, Hours = 8.1m },
                new SupportDayStatViewModel { Day = "Tue", Reports = 476, Hours = 8.4m },
                new SupportDayStatViewModel { Day = "Wed", Reports = 391, Hours = 7.8m },
                new SupportDayStatViewModel { Day = "Thu", Reports = 512, Hours = 9.2m },
                new SupportDayStatViewModel { Day = "Fri", Reports = 449, Hours = 8.6m },
                new SupportDayStatViewModel { Day = "Sat", Reports = 318, Hours = 6.3m },
                new SupportDayStatViewModel { Day = "Sun", Reports = 286, Hours = 4.5m }
            ];
        }
    }
}
