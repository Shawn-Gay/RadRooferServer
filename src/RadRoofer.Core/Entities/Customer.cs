using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class Customer : BaseEntity, ISoftDeletable, IOrganizationIsolated, IServiceLocationIsolated, ISyncedEntity
{
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public required string LastName { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    public LeadSource LeadSource { get; set; }

    public CustomerType CustomerType { get; set; }

    [MaxLength(200)]
    public string? ExternalId { get; set; }

    public DateTimeOffset? LastSynced { get; set; }

    public Organization Organization { get; set; } = null!;
    public ServiceLocation ServiceLocation { get; set; } = null!;
    public ICollection<ContactInfo> ContactInfos { get; set; } = new List<ContactInfo>();
    public ICollection<PhysicalLocation> PhysicalLocations { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<CallLog> CallLogs { get; set; } = [];
}
