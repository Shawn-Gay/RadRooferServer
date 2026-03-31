namespace RadRoofer.Core.Entities;

public class LocationSchedule : BaseEntity, IOrganizationIsolated, IServiceLocationIsolated
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly WorkStart { get; set; }
    public TimeOnly WorkEnd { get; set; }

    /// <summary>
    /// When false, this day has no work hours — AI is on all day.
    /// </summary>
    public bool IsWorkDay { get; set; } = true;

    public Guid OrganizationId { get; set; }
    public Guid ServiceLocationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public ServiceLocation ServiceLocation { get; set; } = null!;
}
