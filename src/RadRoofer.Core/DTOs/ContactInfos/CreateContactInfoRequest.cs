using RadRoofer.Core.Enums;

namespace RadRoofer.Core.DTOs.ContactInfos;

public record CreateContactInfoRequest
{
    public required PolymorphicEntityType EntityType { get; init; }
    public required Guid EntityId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
}
