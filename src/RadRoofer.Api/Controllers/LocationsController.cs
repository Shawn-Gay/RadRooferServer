namespace RadRoofer.Api.Controllers;

[ApiController]
[Route("v1/locations")]
[Authorize]
public class LocationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken ct)
    {
        try
        {

        var items = await db.ServiceLocations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new
            {
                o.Id,
                o.Name,
                IsActive = o.Status == ServiceLocationStatus.Open,
                o.VapiEnabled,
                o.CalendarId,
            })
            .ToListAsync(ct);
            
            return Ok(new { items });
        }catch(Exception ex)
        {
                return StatusCode(500, new { error = "An error occurred while fetching locations.", details = ex.Message });
        }

    }

    [HttpPut("{id:guid}/assistant")]
    public async Task<IActionResult> ToggleAssistant(
        Guid id,
        [FromBody] ToggleAssistantRequest req,
        CancellationToken ct)
    {
        ServiceLocation? location = await db.ServiceLocations
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (location is null) return NotFound();

        location.VapiEnabled = req.Enabled;
        await db.SaveChangesAsync(ct);

        return Ok(new { location.Id, location.VapiEnabled });
    }

    [HttpPut("{id:guid}/integrations")]
    public async Task<IActionResult> UpdateIntegrations(
        Guid id,
        [FromBody] UpdateIntegrationsRequest req,
        CancellationToken ct)
    {
        ServiceLocation? location = await db.ServiceLocations
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (location is null) return NotFound();

        location.CalendarId = string.IsNullOrWhiteSpace(req.CalendarId) ? null : req.CalendarId.Trim();
        await db.SaveChangesAsync(ct);

        return Ok(new { location.Id, location.CalendarId });
    }
}

public record ToggleAssistantRequest
{
    public required bool Enabled { get; init; }
}

public record UpdateIntegrationsRequest
{
    public string? CalendarId { get; init; }
}
