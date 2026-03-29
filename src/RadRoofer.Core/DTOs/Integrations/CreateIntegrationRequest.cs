using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.Integrations;

public record CreateIntegrationRequest
{
    public required Guid ServiceLocationId { get; init; }
    public IntegrationProvider Provider { get; init; }
    public IntegrationCategory Category { get; init; }
    public string? ConfigJson { get; init; }
}
