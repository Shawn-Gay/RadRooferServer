namespace RadRoofer.Core.DTOs.ContactInfos;

public record UpdateContactInfoRequest
{
    public string? Phone { get; init; }
    public string? Email { get; init; }
}
