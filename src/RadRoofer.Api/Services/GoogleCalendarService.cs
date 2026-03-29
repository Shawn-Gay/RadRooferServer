using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using RadRoofer.Api.Settings;

namespace RadRoofer.Api.Services;

public sealed class GoogleCalendarService(IOptions<GoogleCalendarSettings> options)
{
    private readonly GoogleCalendarSettings _settings = options.Value;

    /// <param name="calendarId">Per-location override. Falls back to the global setting if null.</param>
    public async Task<string?> CreateEventAsync(
        string? calendarId,
        string title,
        string description,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct)
    {
        string targetCalendar = calendarId ?? _settings.CalendarId;
        CalendarService client = BuildClient();

        var @event = new Event
        {
            Summary = title,
            Description = description,
            Start = new EventDateTime { DateTime = startUtc.UtcDateTime, TimeZone = "UTC" },
            End = new EventDateTime { DateTime = endUtc.UtcDateTime, TimeZone = "UTC" },
        };

        Event created = await client.Events
            .Insert(@event, targetCalendar)
            .ExecuteAsync(ct);

        return created.Id;
    }

    private CalendarService BuildClient()
    {
        GoogleCredential credential = GoogleCredential
            .FromJson(_settings.ResolveServiceAccountJson())
            .CreateScoped(CalendarService.Scope.Calendar);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "RadRoofer",
        });
    }
}
