namespace RadRoofer.Core.DTOs.Analytics;

public record AnalyticsSummaryDto
{
    public required int CallsToday { get; init; }
    public required int CallsThisWeek { get; init; }
    public required int BookedThisWeek { get; init; }
    public required int UnbookedThisWeek { get; init; }
    public required int AfterHoursThisWeek { get; init; }
    public required double? AvgDurationBookedSeconds { get; init; }
    public required double? AvgDurationUnbookedSeconds { get; init; }
}
