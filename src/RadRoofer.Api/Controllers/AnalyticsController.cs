using RadRoofer.Core.DTOs.Analytics;
using RadRoofer.Core.Entities;

namespace RadRoofer.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
[Authorize]
public class AnalyticsController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] Guid? locationId,
        CancellationToken ct)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset todayUtc = now.Date;
        DateTimeOffset weekStart = todayUtc.AddDays(-(int)todayUtc.DayOfWeek);

        IQueryable<CallLog> query = db.CallLogs.AsNoTracking();
        if (locationId.HasValue)
        {
            query = query.Where(o => o.ServiceLocationId == locationId.Value);
        }

        int callsToday = await query.CountAsync(o => o.StartedAt >= todayUtc, ct);

        var weekCalls = await query
            .Where(o => o.StartedAt >= weekStart)
            .Select(o => new
            {
                o.StartedAt,
                o.EndedAt,
                IsBooked = EF.Property<Guid?>(o, "AppointmentId") != null,
            })
            .ToListAsync(ct);

        int callsThisWeek = weekCalls.Count;
        int bookedThisWeek = weekCalls.Count(o => o.IsBooked);
        int unbookedThisWeek = weekCalls.Count(o => !o.IsBooked);

        var bookedDurations = weekCalls
            .Where(o => o.IsBooked && o.EndedAt.HasValue)
            .Select(o => (o.EndedAt!.Value - o.StartedAt).TotalSeconds)
            .ToList();

        var unbookedDurations = weekCalls
            .Where(o => !o.IsBooked && o.EndedAt.HasValue)
            .Select(o => (o.EndedAt!.Value - o.StartedAt).TotalSeconds)
            .ToList();

        int afterHoursThisWeek = await ComputeAfterHoursAsync(locationId, weekCalls.Select(o => o.StartedAt).ToList(), ct);

        return Ok(new AnalyticsSummaryDto
        {
            CallsToday = callsToday,
            CallsThisWeek = callsThisWeek,
            BookedThisWeek = bookedThisWeek,
            UnbookedThisWeek = unbookedThisWeek,
            AfterHoursThisWeek = afterHoursThisWeek,
            AvgDurationBookedSeconds = bookedDurations.Count > 0 ? bookedDurations.Average() : null,
            AvgDurationUnbookedSeconds = unbookedDurations.Count > 0 ? unbookedDurations.Average() : null,
        });
    }

    private async Task<int> ComputeAfterHoursAsync(
        Guid? locationId,
        List<DateTimeOffset> callTimes,
        CancellationToken ct)
    {
        if (locationId is null || callTimes.Count == 0)
        {
            return 0;
        }

        var location = await db.ServiceLocations
            .AsNoTracking()
            .Where(o => o.Id == locationId.Value)
            .Select(o => new { o.Timezone })
            .FirstOrDefaultAsync(ct);

        if (location is null)
        {
            return 0;
        }

        List<LocationSchedule> schedule = await db.LocationSchedules
            .AsNoTracking()
            .Where(o => o.ServiceLocationId == locationId.Value)
            .ToListAsync(ct);

        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(location.Timezone);

        return callTimes.Count(startedAt =>
        {
            DateTimeOffset local = TimeZoneInfo.ConvertTime(startedAt, tz);
            LocationSchedule? day = schedule.FirstOrDefault(o => o.DayOfWeek == local.DayOfWeek);

            if (day is null || !day.IsWorkDay)
            {
                return true;
            }

            TimeOnly timeOfDay = TimeOnly.FromTimeSpan(local.TimeOfDay);
            return timeOfDay < day.WorkStart || timeOfDay >= day.WorkEnd;
        });
    }
}
