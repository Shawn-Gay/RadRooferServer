namespace RadRoofer.Core.DTOs.Integrations;

public record IntegrationDto
{
    public required Guid Id { get; init; }
    public required Guid ServiceLocationId { get; init; }
    public required string Provider { get; init; }
    public required string Category { get; init; }
    public required string Status { get; init; }
    public string? ConfigJson { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
