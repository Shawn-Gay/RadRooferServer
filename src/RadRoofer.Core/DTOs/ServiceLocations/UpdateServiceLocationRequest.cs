using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.ServiceLocations;

public record UpdateServiceLocationRequest
{
    public required string Name { get; init; }
    public required string Timezone { get; init; }
    public ServiceLocationStatus Status { get; init; }
    public string? SystemPromptBase { get; init; }
    public string? BusinessHoursJson { get; init; }
    public string? PricingNotes { get; init; }
    public string? FaqJson { get; init; }
    public bool OffersFreeInspections { get; init; }
}
