using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class Appointment : BaseEntity, IOrganizationIsolated, IServiceLocationIsolated, ISyncedEntity
{
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public required DateTimeOffset StartTime { get; set; }

    public required DateTimeOffset EndTime { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? ExternalId { get; set; }

    public DateTimeOffset? LastSynced { get; set; }

    public Organization Organization { get; set; } = null!;
    public ServiceLocation ServiceLocation { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Employee? Employee { get; set; }
    public Service? Service { get; set; }
}
