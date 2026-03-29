namespace RadRoofer.Api.Settings;

public sealed class GoogleCalendarSettings
{
    // Full service account JSON string — set via env var GoogleCalendar__ServiceAccountJson
    // For local dev, use ServiceAccountJsonPath instead to point to the downloaded key file
    public string? ServiceAccountJson { get; init; }

    // Path to the service account JSON key file — local dev only
    public string? ServiceAccountJsonPath { get; init; }

    // Calendar ID to write events to — set via env var GoogleCalendar__CalendarId
    public required string CalendarId { get; init; }

    public string ResolveServiceAccountJson() =>
        !string.IsNullOrEmpty(ServiceAccountJson)
            ? ServiceAccountJson
            : !string.IsNullOrEmpty(ServiceAccountJsonPath)
                ? File.ReadAllText(ServiceAccountJsonPath)
                : throw new InvalidOperationException(
                    "Either GoogleCalendar:ServiceAccountJson or GoogleCalendar:ServiceAccountJsonPath must be set.");
}
