namespace RadRoofer.Core.DTOs.Organizations;

public record OrganizationDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public OrganizationDetailsDto? Details { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

public record OrganizationDetailsDto
{
    public required Guid Id { get; init; }
    public string? TaxId { get; init; }
    public string? Website { get; init; }
    public string? PrimaryIndustry { get; init; }
}
