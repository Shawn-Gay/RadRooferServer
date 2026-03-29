namespace RadRoofer.Core.Entities;

/// <summary>
/// Marker interface for entities that belong to a specific ServiceLocation.
/// No tenant logic is attached — this is a structural marker for future use
/// (e.g., generic location-scoped queries, admin tooling, reporting).
/// LocationId is a shadow FK on most implementors; DispatchMessage is the exception.
/// </summary>
public interface IServiceLocationIsolated { }
