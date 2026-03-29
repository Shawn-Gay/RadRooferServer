using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.Organizations;

public record UpdateOrganizationRequest
{
    public required string Name { get; init; }
    public OrganizationStatus Status { get; init; }
    public string? TaxId { get; init; }
    public string? Website { get; init; }
    public PrimaryIndustry PrimaryIndustry { get; init; }
}
