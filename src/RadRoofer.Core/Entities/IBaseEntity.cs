namespace RadRoofer.Core.Entities;

public interface IBaseEntity : ISoftDeletable
{
    Guid Id { get; set; }
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    DateTimeOffset? SoftDeletedAt { get; set; }
}
