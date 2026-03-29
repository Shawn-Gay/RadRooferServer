namespace RadRoofer.Core.DTOs.ServiceLocations;

public record ServiceLocationDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Timezone { get; init; }
    public required string Status { get; init; }
    public ServiceLocationDetailsDto? Details { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

public record ServiceLocationDetailsDto
{
    public required Guid Id { get; init; }
    public string? SystemPromptBase { get; init; }
    public string? BusinessHoursJson { get; init; }
    public string? PricingNotes { get; init; }
    public string? FaqJson { get; init; }
    public bool OffersFreeInspections { get; init; }
}
