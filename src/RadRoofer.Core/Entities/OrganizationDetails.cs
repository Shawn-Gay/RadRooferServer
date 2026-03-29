using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class OrganizationDetails : BaseEntity, IOrganizationIsolated
{
    [MaxLength(50)]
    public string? TaxId { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    public PrimaryIndustry PrimaryIndustry { get; set; }

    public Organization Organization { get; set; } = null!;
}
