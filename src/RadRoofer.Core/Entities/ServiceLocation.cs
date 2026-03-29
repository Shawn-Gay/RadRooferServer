using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class ServiceLocation : BaseEntity, IOrganizationIsolated
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public required string Timezone { get; set; }

    public ServiceLocationStatus Status { get; set; } = ServiceLocationStatus.Open;

    public bool VapiEnabled { get; set; } = true;

    [MaxLength(200)]
    public string? VapiSecret { get; set; }

    // Overrides the global GoogleCalendar:CalendarId setting for this location
    [MaxLength(200)]
    public string? CalendarId { get; set; }

    public Organization Organization { get; set; } = null!;
    public ServiceLocationDetails? Details { get; set; }
    public ICollection<Integration> Integrations { get; set; } = [];
    public ICollection<Employee> Employees { get; set; } = [];
    public ICollection<Service> Services { get; set; } = [];
    public ICollection<CallLog> CallLogs { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<PhysicalLocation> PhysicalLocations { get; set; } = [];
    public ICollection<ContactInfo> ContactInfos { get; set; } = new List<ContactInfo>();
}
