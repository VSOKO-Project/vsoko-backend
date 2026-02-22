using Application.Interfaces.CachingManager;
using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Caching;

public class HybridCacheWrapper : ICacheService
{
    private readonly HybridCache _hybridCache;

    public HybridCacheWrapper(HybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        CancellationToken cancellationToken = default) where T : class
    {
        return await _hybridCache.GetOrCreateAsync(
            key,
            factory,
            cancellationToken: cancellationToken
        );
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _hybridCache.RemoveAsync(key, cancellationToken);
    }
}