namespace RadRoofer.Core.DTOs.Calls;

public record CallLogDto
{
    public required Guid Id { get; init; }
    public required Guid ServiceLocationId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? AppointmentId { get; init; }
    public required string VapiCallId { get; init; }
    public required string Direction { get; init; }
    public required string Status { get; init; }
    public string? RecordingUrl { get; init; }
    public string? Transcript { get; init; }
    public string? Summary { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
