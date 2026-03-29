namespace RadRoofer.Core.Entities;

public interface ISyncedEntity
{
    static readonly TimeSpan DefaultSyncWaitDuration = TimeSpan.FromMinutes(10);

    DateTimeOffset? LastSynced { get; set; }
}

public static class SyncedEntityExtensions
{
    public static bool IsStale(this ISyncedEntity entity, TimeSpan? waitDuration = null)
        => entity.LastSynced == null
           || DateTimeOffset.UtcNow - entity.LastSynced.Value > (waitDuration ?? ISyncedEntity.DefaultSyncWaitDuration);
}
