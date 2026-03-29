namespace RadRoofer.Core.Entities;

public abstract class BaseEntity : IBaseEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? SoftDeletedAt { get; set; }
}
