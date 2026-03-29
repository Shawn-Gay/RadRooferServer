namespace RadRoofer.Core.DTOs.Appointments;

public record AppointmentDto
{
    public required Guid Id { get; init; }
    public required Guid ServiceLocationId { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public string? Notes { get; init; }
    public string? GoogleEventId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
