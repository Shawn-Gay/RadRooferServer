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
        {
            return Ok();
        }

        var call = payload.Message.Call;
        if (call is null)
        {
            return Ok();
        }

        // Idempotency: VapiCallId has a unique index — skip if already processed
        bool alreadyProcessed = await db.CallLogs
            .IgnoreQueryFilters()
            .AnyAsync(o => o.VapiCallId == call.Id, ct);
        if (alreadyProcessed)
        {
            return Ok();
        }

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

        if (location is null)
        {
            return Ok();
        }

        if (!location.VapiEnabled)
        {
            return Ok(new { status = "disabled" });
        }

        CallStatus callStatus = MapEndedReason(payload.Message.EndedReason);

        DateTimeOffset startTime = call.StartedAt ?? DateTimeOffset.UtcNow;
        DateTimeOffset endTime = call.EndedAt ?? startTime.AddHours(1);

        Appointment? appointment = null;
        string? googleEventId = null;

        if (callStatus == CallStatus.AssistantEnded)
        {
            var data = payload.Message.Analysis?.StructuredData;
            string? callerName = data?.CallerName;
            string? callerPhone = data?.CallerPhone ?? call.Customer?.Number;
            string? address = data?.Address;
            string? reason = data?.ReasonForCall;

            string notes = BuildNotes(callerName, callerPhone, address, reason, payload.Message.Analysis?.Summary);

            googleEventId = await calendar.CreateEventAsync(
                calendarId: location.CalendarId,
                title: $"Roofing Inquiry — {callerName ?? callerPhone ?? "Unknown"}",
                description: notes,
                startUtc: startTime,
                endUtc: endTime,
                ct: ct);

            appointment = new Appointment
            {
                StartTime = startTime,
                EndTime = endTime,
                Notes = notes,
                ExternalId = googleEventId,
                Status = AppointmentStatus.Scheduled,
            };

            appointment.OrganizationId = location.OrganizationId;
            appointment.ServiceLocationId = locationId;
            db.Appointments.Add(appointment);
        }

        var callLog = new CallLog
        {
            VapiCallId = call.Id,
            Direction = CallDirection.Inbound,
            Status = callStatus,
            Transcript = payload.Message.Artifact?.Transcript,
            Summary = payload.Message.Analysis?.Summary,
            StartedAt = startTime,
            EndedAt = call.EndedAt,
            Appointment = appointment,
        };

        callLog.OrganizationId = location.OrganizationId;
        callLog.ServiceLocationId = locationId;
        db.CallLogs.Add(callLog);

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

        return Ok(new { appointmentId = appointment?.Id, googleEventId });
    }

    private static CallStatus MapEndedReason(string? reason) => reason switch
    {
        // ── Assistant wrapped up normally ────────────────────────────────────
        "assistant-ended-call"                                            => CallStatus.AssistantEnded,
        "assistant-ended-call-after-message-spoken"                       => CallStatus.AssistantEnded,
        "assistant-ended-call-with-hangup-task"                           => CallStatus.AssistantEnded,
        "assistant-said-end-call-phrase"                                  => CallStatus.AssistantEnded,
        "exceeded-max-duration"                                           => CallStatus.AssistantEnded,
        "manually-canceled"                                               => CallStatus.AssistantEnded,
        "call.ending.hook-executed-say"                                   => CallStatus.AssistantEnded,
        "call.in-progress.twilio-completed-call"                          => CallStatus.AssistantEnded,
        "vonage-completed"                                                => CallStatus.AssistantEnded,
        "call.in-progress.sip-completed-call"                             => CallStatus.AssistantEnded,

        // ── Customer hung up ─────────────────────────────────────────────────
        "customer-ended-call"                                             => CallStatus.CustomerHungUp,
        "customer-ended-call-before-warm-transfer"                        => CallStatus.CustomerHungUp,
        "customer-ended-call-after-warm-transfer-attempt"                 => CallStatus.CustomerHungUp,
        "customer-ended-call-during-transfer"                             => CallStatus.CustomerHungUp,

        // ── Transferred / forwarded ──────────────────────────────────────────
        "assistant-forwarded-call"                                        => CallStatus.Transferred,
        "call.ending.hook-executed-transfer"                              => CallStatus.Transferred,
        "call.ringing.hook-executed-transfer"                             => CallStatus.Transferred,

        // ── Voicemail ────────────────────────────────────────────────────────
        "voicemail"                                                       => CallStatus.Voicemail,

        // ── No answer ────────────────────────────────────────────────────────
        "customer-did-not-answer"                                         => CallStatus.NoAnswer,
        "assistant-join-timed-out"                                        => CallStatus.NoAnswer,

        // ── Busy ─────────────────────────────────────────────────────────────
        "customer-busy"                                                   => CallStatus.Busy,
        "call.forwarding.operator-busy"                                   => CallStatus.Busy,

        // ── Caller went silent ───────────────────────────────────────────────
        "silence-timed-out"                                               => CallStatus.SilenceTimeout,

        // ── Failed to connect at ringing stage ───────────────────────────────
        "customer-did-not-give-microphone-permission"                     => CallStatus.ConnectionFailed,
        "call.in-progress.error-assistant-did-not-receive-customer-audio" => CallStatus.ConnectionFailed,
        "twilio-failed-to-connect-call"                                   => CallStatus.ConnectionFailed,
        "twilio-reported-customer-misdialed"                              => CallStatus.ConnectionFailed,
        "vonage-disconnected"                                             => CallStatus.ConnectionFailed,
        "vonage-failed-to-connect-call"                                   => CallStatus.ConnectionFailed,
        "vonage-rejected"                                                 => CallStatus.ConnectionFailed,
        "call.ringing.error-sip-inbound-call-failed-to-connect"           => CallStatus.ConnectionFailed,
        "call.ringing.sip-inbound-caller-hungup-before-call-connect"      => CallStatus.ConnectionFailed,
        "call.ringing.hook-executed-say"                                  => CallStatus.ConnectionFailed,

        // ── Technical failure (call.start.error-*, pipeline-error-*, ─────────
        //    *-voice-failed, *-transcriber-failed, transport, worker-shutdown) ─
        _                                                                 => CallStatus.Failed,
    };

    private static string BuildNotes(
        string? callerName,
        string? callerPhone,
        string? address,
        string? reason,
        string? summary)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(callerName))
        {
            parts.Add($"Name: {callerName}");
        }
        if (!string.IsNullOrEmpty(callerPhone))
        {
            parts.Add($"Phone: {callerPhone}");
        }
        if (!string.IsNullOrEmpty(address))
        {
            parts.Add($"Address: {address}");
        }
        if (!string.IsNullOrEmpty(reason))
        {
            parts.Add($"Reason: {reason}");
        }
        if (!string.IsNullOrEmpty(summary))
        {
            parts.Add($"\nSummary: {summary}");
        }
        return string.Join("\n", parts);
    }
}
