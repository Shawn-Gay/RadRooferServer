using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.Integrations;

public record UpdateIntegrationRequest
{
    public IntegrationStatus Status { get; init; }
    public string? ConfigJson { get; init; }
}
