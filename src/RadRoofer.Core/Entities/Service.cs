using System.ComponentModel.DataAnnotations;

namespace RadRoofer.Core.Entities;

public class Service : BaseEntity, IOrganizationIsolated, IServiceLocationIsolated
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int EstimatedDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public Organization Organization { get; set; } = null!;
    public ServiceLocation ServiceLocation { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
}
