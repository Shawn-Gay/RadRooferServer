using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace RadRoofer.Api.Controllers;

[ApiController]
[Route("v1/calls")]
[Authorize]
public class CallsController(AppDbContext db, IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpPost("outbound")]
    public async Task<IActionResult> InitiateOutbound(
        [FromBody] InitiateOutboundRequest req,
        CancellationToken ct)
    {
        ServiceLocation? location = req.LocationId.HasValue
            ? await db.ServiceLocations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == req.LocationId.Value, ct)
            : await db.ServiceLocations.AsNoTracking().FirstOrDefaultAsync(ct);

        if (location is null) return NotFound(new { error = "Location not found." });

        if (string.IsNullOrWhiteSpace(location.VapiAssistantId) ||
            string.IsNullOrWhiteSpace(location.VapiPhoneNumberId))
            return BadRequest(new { error = "Vapi assistant and phone number must be configured for this location." });

        HttpClient client = httpClientFactory.CreateClient("vapi");

        var payload = new
        {
            assistantId = location.VapiAssistantId,
            phoneNumberId = location.VapiPhoneNumberId,
            customer = new { number = req.PhoneNumber },
        };

        StringContent content = new(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await client.PostAsync("call", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(ct);
            return StatusCode((int)response.StatusCode, new { error = "Vapi rejected the request.", details = body });
        }

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        string? callId = doc.RootElement.TryGetProperty("id", out JsonElement idEl) ? idEl.GetString() : null;

        return Ok(new { callId });
    }
}

public record InitiateOutboundRequest
{
    [Required]
    [Phone]
    public required string PhoneNumber { get; init; }

    public Guid? LocationId { get; init; }
}
