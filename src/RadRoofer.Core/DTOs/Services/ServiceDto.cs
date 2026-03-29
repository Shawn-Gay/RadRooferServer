namespace RadRoofer.Core.DTOs.Services;

public record ServiceDto
{
    public required Guid Id { get; init; }
    public required Guid ServiceLocationId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int EstimatedDurationMinutes { get; init; }
    public bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
