namespace RadRoofer.Core.DTOs.Services;

public record CreateServiceRequest
{
    public required Guid ServiceLocationId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public bool IsActive { get; init; } = true;
}
