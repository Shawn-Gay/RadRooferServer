using RadRoofer.Core.DTOs.Schedule;
using RadRoofer.Core.Entities;

namespace RadRoofer.Api.Controllers;

[ApiController]
[Route("v1/locations")]
[Authorize]
public class LocationScheduleController(AppDbContext db) : ControllerBase
{
    [HttpGet("{locationId:guid}/schedule")]
    public async Task<IActionResult> GetSchedule(Guid locationId, CancellationToken ct)
    {
        List<LocationScheduleDto> rows = await db.LocationSchedules
            .AsNoTracking()
            .Where(o => o.ServiceLocationId == locationId)
            .OrderBy(o => o.DayOfWeek)
            .Select(o => new LocationScheduleDto
            {
                DayOfWeek = o.DayOfWeek,
                WorkStart = o.WorkStart,
                WorkEnd = o.WorkEnd,
                IsWorkDay = o.IsWorkDay,
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpPut("{locationId:guid}/schedule")]
    public async Task<IActionResult> UpsertSchedule(
        Guid locationId,
        [FromBody] UpsertScheduleRequest req,
        CancellationToken ct)
    {
        Guid? orgId = await db.ServiceLocations
            .Where(o => o.Id == locationId)
            .Select(o => (Guid?)EF.Property<Guid>(o, "OrganizationId"))
            .FirstOrDefaultAsync(ct);

        if (orgId is null)
        {
            return NotFound();
        }

        // Hard delete bypasses the soft-delete SaveChanges override
        await db.LocationSchedules
            .Where(o => o.ServiceLocationId == locationId)
            .ExecuteDeleteAsync(ct);

        List<LocationSchedule> newRows = req.Days.Select(d => new LocationSchedule
        {
            OrganizationId = orgId.Value,
            ServiceLocationId = locationId,
            DayOfWeek = d.DayOfWeek,
            WorkStart = d.WorkStart,
            WorkEnd = d.WorkEnd,
            IsWorkDay = d.IsWorkDay,
        }).ToList();

        db.LocationSchedules.AddRange(newRows);
        await db.SaveChangesAsync(ct);

        return Ok(newRows.Select(o => new LocationScheduleDto
        {
            DayOfWeek = o.DayOfWeek,
            WorkStart = o.WorkStart,
            WorkEnd = o.WorkEnd,
            IsWorkDay = o.IsWorkDay,
        }));
    }
}
