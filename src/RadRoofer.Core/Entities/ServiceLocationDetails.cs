using System.ComponentModel.DataAnnotations;

namespace RadRoofer.Core.Entities;

public class ServiceLocationDetails : BaseEntity, IOrganizationIsolated, IServiceLocationIsolated
{
    public string? SystemPromptBase { get; set; }

    public string? BusinessHoursJson { get; set; }

    [MaxLength(2000)]
    public string? PricingNotes { get; set; }

    public string? FaqJson { get; set; }

    public bool OffersFreeInspections { get; set; }

    public Organization Organization { get; set; } = null!;
    public ServiceLocation ServiceLocation { get; set; } = null!;
}
