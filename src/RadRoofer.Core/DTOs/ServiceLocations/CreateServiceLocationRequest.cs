namespace RadRoofer.Core.DTOs.ServiceLocations;

public record CreateServiceLocationRequest
{
    public required string Name { get; init; }
    public required string Timezone { get; init; }
}
