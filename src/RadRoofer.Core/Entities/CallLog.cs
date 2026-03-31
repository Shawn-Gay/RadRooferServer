using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class CallLog : BaseEntity, IOrganizationIsolated, IServiceLocationIsolated
{
    [MaxLength(200)]
    public required string VapiCallId { get; set; }

    public CallDirection Direction { get; set; }

    public CallStatus Status { get; set; }

    [MaxLength(500)]
    public string? RecordingUrl { get; set; }

    public string? Transcript { get; set; }

    [MaxLength(2000)]
    public string? Summary { get; set; }

    public required DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public Guid OrganizationId { get; set; }
    public Guid ServiceLocationId { get; set; }

    public Organization Organization { get; set; } = null!;
    public ServiceLocation ServiceLocation { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Appointment? Appointment { get; set; }
}
