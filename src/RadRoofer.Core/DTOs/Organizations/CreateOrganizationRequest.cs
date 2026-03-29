namespace RadRoofer.Core.DTOs.Organizations;

public record CreateOrganizationRequest
{
    public required string Name { get; init; }
}
