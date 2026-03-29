using RadRoofer.Core.Enums;

namespace RadRoofer.Core.Entities;

public class Integration : BaseEntity, IOrganizationIsolated, IServiceLocationIsolated
{
    public IntegrationProvider Provider { get; set; }

    public IntegrationCategory Category { get; set; }

    public string? ConfigJson { get; set; }

    public IntegrationStatus Status { get; set; } = IntegrationStatus.Disconnected;

    public Organization Organization { get; set; } = null!;
    public ServiceLocation ServiceLocation { get; set; } = null!;
}
