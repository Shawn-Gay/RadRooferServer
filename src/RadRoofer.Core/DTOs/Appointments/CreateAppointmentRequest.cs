using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.Appointments;

public record CreateAppointmentRequest
{
    public required Guid ServiceLocationId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? EmployeeId { get; init; }
    public Guid? ServiceId { get; init; }
    public AppointmentStatus Status { get; init; } = AppointmentStatus.Scheduled;
    public required DateTimeOffset StartTime { get; init; }
    public required DateTimeOffset EndTime { get; init; }
    public string? Notes { get; init; }
    public string? ExternalId { get; init; }
}
