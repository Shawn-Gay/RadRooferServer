using RadRoofer.Core.DTOs.Calls;

namespace RadRoofer.Api.Controllers;

[ApiController]
[Route("v1/call-logs")]
[Authorize]
public class CallLogsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? locationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        IQueryable<CallLog> query = db.CallLogs
            .AsNoTracking()
            .OrderByDescending(o => o.StartedAt);

        if (locationId.HasValue)
            query = query.Where(o => o.ServiceLocation.Id == locationId.Value);

        int total = await query.CountAsync(ct);
        int take = Math.Min(pageSize, 100);

        List<CallLogDto> items = await query
            .Skip((page - 1) * take)
            .Take(take)
            .Select(o => new CallLogDto
            {
                Id = o.Id,
                VapiCallId = o.VapiCallId,
                ServiceLocationId = o.ServiceLocation.Id,
                Direction = o.Direction.ToString(),
                Status = o.Status.ToString(),
                StartedAt = o.StartedAt,
                EndedAt = o.EndedAt,
                Summary = o.Summary,
                Transcript = o.Transcript,
                RecordingUrl = o.RecordingUrl,
                CreatedAt = o.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(new { items, page, pageSize = take, totalCount = total });
    }
}
