using RadRoofer.Core.DTOs.Appointments;

namespace RadRoofer.Api.Controllers;

[ApiController]
[Route("v1/appointments")]
[Authorize]
public class AppointmentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? locationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var query = db.Appointments
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt);

        var filtered = locationId.HasValue
            ? query.Where(o => o.ServiceLocation.Id == locationId.Value)
            : query;

        int total = await filtered.CountAsync(ct);
        int take = Math.Min(pageSize, 100);

        List<AppointmentDto> items = await filtered
            .Skip((page - 1) * take)
            .Take(take)
            .Select(o => new AppointmentDto
            {
                Id = o.Id,
                ServiceLocationId = o.ServiceLocation.Id,
                Status = o.Status.ToString(),
                StartTime = o.StartTime,
                EndTime = o.EndTime,
                Notes = o.Notes,
                GoogleEventId = o.ExternalId,
                CreatedAt = o.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(new { items, page, pageSize = take, totalCount = total });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        AppointmentDto? appt = await db.Appointments
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new AppointmentDto
            {
                Id = o.Id,
                ServiceLocationId = o.ServiceLocation.Id,
                Status = o.Status.ToString(),
                StartTime = o.StartTime,
                EndTime = o.EndTime,
                Notes = o.Notes,
                GoogleEventId = o.ExternalId,
                CreatedAt = o.CreatedAt,
            })
            .FirstOrDefaultAsync(ct);

        return appt is null ? NotFound() : Ok(appt);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] Guid? locationId,
        CancellationToken ct)
    {
        IQueryable<Appointment> query = db.Appointments.AsNoTracking();

        if (locationId.HasValue)
        {
            query = query.Where(o => o.ServiceLocation.Id == locationId.Value);
        }

        DateTimeOffset today = DateTimeOffset.UtcNow.Date;
        DateTimeOffset weekStart = today.AddDays(-(int)today.DayOfWeek);

        int todayCount = await query.CountAsync(o => o.CreatedAt >= today, ct);
        int weekCount = await query.CountAsync(o => o.CreatedAt >= weekStart, ct);

        return Ok(new { today = todayCount, thisWeek = weekCount });
    }
}
