using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class Employee : BaseEntity, IOrganizationIsolated, IServiceLocationIsolated
{
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [MaxLength(100)]
    public required string LastName { get; set; }

    public EmployeeRole Role { get; set; }

    public bool IsBookable { get; set; }

    public Organization Organization { get; set; } = null!;
    public ServiceLocation ServiceLocation { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<ContactInfo> ContactInfos { get; set; } = new List<ContactInfo>();
    public ICollection<PhysicalLocation> PhysicalLocations { get; set; } = [];
}
