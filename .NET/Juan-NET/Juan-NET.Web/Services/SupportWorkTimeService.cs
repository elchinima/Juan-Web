namespace Juan_NET.Web.Services
{
    public class SupportWorkTimeService
    {
        public const int MaxShiftSeconds = 12 * 60 * 60;

        private static readonly TimeSpan SupportLocalOffset = TimeSpan.FromHours(4);

        private readonly AppDbContext _context;

        public SupportWorkTimeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(int Seconds, bool LimitReached)> UpdateShiftAsync(int userId, bool shouldRun)
        {
            var now = DateTime.UtcNow;
            var workDate = now.Add(SupportLocalOffset).Date;
            var shift = await _context.SupportOperatorWorkTimes
                .FirstOrDefaultAsync(item => item.OperatorUserId == userId && item.WorkDate == workDate);

            if (shift is null && !shouldRun)
            {
                return (0, false);
            }

            if (shift is null)
            {
                shift = new SupportOperatorWorkTime
                {
                    OperatorUserId = userId,
                    WorkDate = workDate,
                    LastStartedAt = shouldRun ? now : null,
                    UpdatedAt = now
                };
                _context.SupportOperatorWorkTimes.Add(shift);
            }
            else if (shift.LastStartedAt.HasValue)
            {
                var secondsToAdd = Math.Max(0, (int)Math.Floor((now - shift.LastStartedAt.Value).TotalSeconds));
                shift.TotalSeconds = Math.Min(MaxShiftSeconds, shift.TotalSeconds + secondsToAdd);
                shift.LastStartedAt = shift.TotalSeconds >= MaxShiftSeconds || !shouldRun ? null : now;
                shift.UpdatedAt = now;
            }

            if (shift.TotalSeconds >= MaxShiftSeconds)
            {
                shift.TotalSeconds = MaxShiftSeconds;
                shift.LastStartedAt = null;
            }
            else if (shouldRun && !shift.LastStartedAt.HasValue)
            {
                shift.LastStartedAt = now;
                shift.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();

            return (shift.TotalSeconds, shift.TotalSeconds >= MaxShiftSeconds);
        }
    }
}
