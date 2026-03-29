using Npgsql;
using RadRoofer.Api.Filters;
using RadRoofer.Api.Services;
using RadRoofer.Core.DTOs.Vapi;

namespace RadRoofer.Api.Controllers;

[ApiController]
[Route("v1/webhooks/vapi")]
[AllowAnonymous]
[ServiceFilter<VapiSecretAuthFilter>]
public class VapiWebhookController(AppDbContext db, GoogleCalendarService calendar) : ControllerBase
{
    [HttpPost("{locationId:guid}")]
    public async Task<IActionResult> Handle(
        Guid locationId,
        [FromBody] VapiWebhookPayload payload,
        CancellationToken ct)
    {
        if (payload.Message.Type != "end-of-call-report")
            return Ok();

        var call = payload.Message.Call;
        if (call is null) return Ok();

        // Idempotency: VapiCallId has a unique index — skip if already processed
        bool alreadyProcessed = await db.CallLogs
            .IgnoreQueryFilters()
            .AnyAsync(o => o.VapiCallId == call.Id, ct);
        if (alreadyProcessed) return Ok();

        var location = await db.ServiceLocations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.Id == locationId)
            .Select(o => new
            {
                o.Id,
                OrganizationId = EF.Property<Guid>(o, "OrganizationId"),
                o.VapiEnabled,
                o.CalendarId,
            })
            .FirstOrDefaultAsync(ct);

        if (location is null) return Ok();
        if (!location.VapiEnabled) return Ok(new { status = "disabled" });

        var data = payload.Message.Analysis?.StructuredData;
        string? callerName = data?.CallerName;
        string? callerPhone = data?.CallerPhone ?? call.Customer?.Number;
        string? address = data?.Address;
        string? reason = data?.ReasonForCall;

        DateTimeOffset startTime = call.StartedAt ?? DateTimeOffset.UtcNow;
        DateTimeOffset endTime = call.EndedAt ?? startTime.AddHours(1);

        string notes = BuildNotes(callerName, callerPhone, address, reason, payload.Message.Analysis?.Summary);

        string? googleEventId = await calendar.CreateEventAsync(
            calendarId: location.CalendarId,
            title: $"Roofing Inquiry — {callerName ?? callerPhone ?? "Unknown"}",
            description: notes,
            startUtc: startTime,
            endUtc: endTime,
            ct: ct);

        var appointment = new Appointment
        {
            StartTime = startTime,
            EndTime = endTime,
            Notes = notes,
            ExternalId = googleEventId,
            Status = AppointmentStatus.Scheduled,
        };

        var callLog = new CallLog
        {
            VapiCallId = call.Id,
            Direction = CallDirection.Inbound,
            Status = CallStatus.Completed,
            Transcript = payload.Message.Artifact?.Transcript,
            Summary = payload.Message.Analysis?.Summary,
            StartedAt = startTime,
            EndedAt = call.EndedAt,
            Appointment = appointment,
        };

        db.Appointments.Add(appointment);
        db.Entry(appointment).Property("OrganizationId").CurrentValue = location.OrganizationId;
        db.Entry(appointment).Property("ServiceLocationId").CurrentValue = locationId;

        db.CallLogs.Add(callLog);
        db.Entry(callLog).Property("OrganizationId").CurrentValue = location.OrganizationId;
        db.Entry(callLog).Property("ServiceLocationId").CurrentValue = locationId;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg
                                            && pg.SqlState == "23505")
        {
            // Concurrent duplicate — already processed
            return Ok();
        }

        return Ok(new { appointmentId = appointment.Id, googleEventId });
    }

    private static string BuildNotes(
        string? callerName,
        string? callerPhone,
        string? address,
        string? reason,
        string? summary)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(callerName)) parts.Add($"Name: {callerName}");
        if (!string.IsNullOrEmpty(callerPhone)) parts.Add($"Phone: {callerPhone}");
        if (!string.IsNullOrEmpty(address)) parts.Add($"Address: {address}");
        if (!string.IsNullOrEmpty(reason)) parts.Add($"Reason: {reason}");
        if (!string.IsNullOrEmpty(summary)) parts.Add($"\nSummary: {summary}");
        return string.Join("\n", parts);
    }
}
