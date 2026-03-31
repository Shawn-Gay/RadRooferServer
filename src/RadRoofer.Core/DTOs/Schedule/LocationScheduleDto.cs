namespace RadRoofer.Core.DTOs.Schedule;

public record LocationScheduleDto
{
    public required DayOfWeek DayOfWeek { get; init; }
    public required TimeOnly WorkStart { get; init; }
    public required TimeOnly WorkEnd { get; init; }
    public required bool IsWorkDay { get; init; }
}
