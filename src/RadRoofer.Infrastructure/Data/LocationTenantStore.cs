using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace RadRoofer.Infrastructure.Data;

public class LocationTenantStore(AppDbContext db) : IMultiTenantStore<TenantInfo>
{
    public async Task<TenantInfo?> TryGetByIdentifierAsync(string identifier)
    {
        if (!Guid.TryParse(identifier, out var locationId)) return null;

        var result = await db.ServiceLocations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.Id == locationId)
            .Select(o => new { OrganizationId = EF.Property<Guid>(o, "OrganizationId") })
            .FirstOrDefaultAsync();

        if (result is null) return null;

        return new TenantInfo
        {
            Id = result.OrganizationId.ToString(),
            Identifier = identifier
        };
    }

    public Task<bool> TryAddAsync(TenantInfo tenantInfo) => Task.FromResult(false);
    public Task<bool> TryRemoveAsync(string identifier) => Task.FromResult(false);
    public Task<bool> TryUpdateAsync(TenantInfo tenantInfo) => Task.FromResult(false);
    public Task<TenantInfo?> TryGetAsync(string id) => Task.FromResult<TenantInfo?>(null);

    public Task<IEnumerable<TenantInfo>> GetAllAsync() =>
        Task.FromResult(Enumerable.Empty<TenantInfo>());
}
