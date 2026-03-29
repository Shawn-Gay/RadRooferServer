using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class Organization : BaseEntity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

    public OrganizationDetails? Details { get; set; }
    public ICollection<ServiceLocation> ServiceLocations { get; set; } = [];
    public ICollection<AppUser> AppUsers { get; set; } = [];
}
