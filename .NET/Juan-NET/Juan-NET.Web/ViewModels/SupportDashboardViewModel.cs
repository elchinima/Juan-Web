namespace Juan_NET.Web.ViewModels
{
    public class SupportDashboardViewModel
    {
        public int DailyReportTarget { get; set; }
        public int DailyBonusReportTarget { get; set; }
        public int WorkHourTarget { get; set; }
        public int BonusWorkHourTarget { get; set; }
        public int TodayReports { get; set; }
        public int WeekReports { get; set; }
        public int OpenReports { get; set; }
        public int ResolvedReports { get; set; }
        public int? ActiveReportId { get; set; }
        public string? ActiveReportCode { get; set; }
        public IReadOnlyList<SupportReportViewModel> RecentReports { get; set; } = [];
        public IReadOnlyList<SupportDayStatViewModel> WeekStats { get; set; } = [];
    }
}
