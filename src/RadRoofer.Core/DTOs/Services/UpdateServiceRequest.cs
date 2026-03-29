namespace RadRoofer.Core.DTOs.Services;

public record UpdateServiceRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public bool IsActive { get; init; }
}
