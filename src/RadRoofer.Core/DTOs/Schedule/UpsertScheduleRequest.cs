namespace RadRoofer.Core.DTOs.Schedule;

public record UpsertScheduleRequest
{
    public required IReadOnlyList<ScheduleDayRequest> Days { get; init; }
}

public record ScheduleDayRequest
{
    public required DayOfWeek DayOfWeek { get; init; }
    public required TimeOnly WorkStart { get; init; }
    public required TimeOnly WorkEnd { get; init; }
    public required bool IsWorkDay { get; init; }
}
