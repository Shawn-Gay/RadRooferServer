namespace RadRoofer.Core.DTOs.PhysicalLocations;

public record PhysicalLocationDto
{
    public required Guid Id { get; init; }
    public required string EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public required string LocationType { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
