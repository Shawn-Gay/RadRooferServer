namespace RadRoofer.Api.Services;

/// <summary>
/// Finbuckle store that trusts the tenant_id from a valid JWT — no DB lookup needed
/// because the JWT is already signed by us.
/// </summary>
public class JwtTenantStore : IMultiTenantStore<TenantInfo>
{
    public Task<TenantInfo?> TryGetByIdentifierAsync(string identifier)
    {
        if (!Guid.TryParse(identifier, out _))
            return Task.FromResult<TenantInfo?>(null);

        return Task.FromResult<TenantInfo?>(new TenantInfo { Id = identifier, Identifier = identifier });
    }

    public Task<TenantInfo?> TryGetAsync(string id)
    {
        if (!Guid.TryParse(id, out _))
            return Task.FromResult<TenantInfo?>(null);

        return Task.FromResult<TenantInfo?>(new TenantInfo { Id = id, Identifier = id });
    }

    public Task<bool> TryAddAsync(TenantInfo tenantInfo) => Task.FromResult(false);
    public Task<bool> TryRemoveAsync(string identifier) => Task.FromResult(false);
    public Task<bool> TryUpdateAsync(TenantInfo tenantInfo) => Task.FromResult(false);
    public Task<IEnumerable<TenantInfo>> GetAllAsync() => Task.FromResult(Enumerable.Empty<TenantInfo>());
}
