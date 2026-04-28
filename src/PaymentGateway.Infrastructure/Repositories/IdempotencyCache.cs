using Microsoft.Extensions.Caching.Memory;

using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Infrastructure.Repositories;

public class IdempotencyCache : IIdempotencyCache
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(24);

    // Using in-memory cache for simplicity. In a real application, consider using a distributed cache like Redis.

    private readonly IMemoryCache _cache;

    public IdempotencyCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<(bool Found, TResponse? Response)> TryGetAsync<TResponse>(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey<TResponse>(idempotencyKey), out TResponse? cached))
            return Task.FromResult((true, cached));

        return Task.FromResult<(bool, TResponse?)>((false, default));
    }

    public Task SetAsync<TResponse>(string idempotencyKey, TResponse response, CancellationToken cancellationToken = default)
    {
        _cache.Set(CacheKey<TResponse>(idempotencyKey), response, DefaultExpiry);
        return Task.CompletedTask;
    }

    private static string CacheKey<TResponse>(string idempotencyKey) =>
        $"idempotency:{typeof(TResponse).Name}:{idempotencyKey}";
}
