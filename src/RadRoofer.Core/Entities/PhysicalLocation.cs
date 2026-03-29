using System.ComponentModel.DataAnnotations;
using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class PhysicalLocation : BaseEntity, IOrganizationIsolated
{
    public PolymorphicEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    [MaxLength(200)]
    public required string AddressLine1 { get; set; }

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [MaxLength(100)]
    public required string City { get; set; }

    [MaxLength(50)]
    public required string State { get; set; }

    [MaxLength(20)]
    public required string ZipCode { get; set; }

    public PhysicalLocationType LocationType { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<ServiceLocation> ServiceLocations { get; set; } = [];
    public ICollection<Employee> Employees { get; set; } = [];
}
