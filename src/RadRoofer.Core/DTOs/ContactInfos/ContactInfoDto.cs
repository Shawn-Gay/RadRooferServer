namespace RadRoofer.Core.DTOs.ContactInfos;

public record ContactInfoDto
{
    public required Guid Id { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
