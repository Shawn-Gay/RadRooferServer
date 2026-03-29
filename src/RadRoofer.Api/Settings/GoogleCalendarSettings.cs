namespace RadRoofer.Api.Settings;

public sealed class GoogleCalendarSettings
{
    public required string CalendarId { get; init; }
    public required string ClientEmail { get; init; }
    public required string PrivateKey { get; init; }
}
