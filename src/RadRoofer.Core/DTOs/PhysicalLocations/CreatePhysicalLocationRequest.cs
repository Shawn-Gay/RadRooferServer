using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.PhysicalLocations;

public record CreatePhysicalLocationRequest
{
    public PolymorphicEntityType EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public required string AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string ZipCode { get; init; }
    public PhysicalLocationType LocationType { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}
