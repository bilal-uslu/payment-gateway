using System.Collections.Concurrent;

using MediatR;

using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Application.Behaviors;

public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private readonly IIdempotencyCache _idempotencyCache;

    public IdempotencyBehavior(IIdempotencyCache idempotencyCache)
    {
        _idempotencyCache = idempotencyCache;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest<TResponse> idempotentRequest)
        {
            return await next();
        }

        var scopedKey = $"{idempotentRequest.MerchantId}:{idempotentRequest.IdempotencyKey}";

        // Fast path: already cached before acquiring the lock.
        var (found, cached) = await _idempotencyCache.TryGetAsync<TResponse>(scopedKey, cancellationToken);

        if (found)
        {
            return cached!;
        }

        var semaphore = _locks.GetOrAdd(scopedKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // Second check: a concurrent request may have populated the cache while we waited.
            (found, cached) = await _idempotencyCache.TryGetAsync<TResponse>(scopedKey, cancellationToken);
            
            if (found)
            {
                return cached!;
            }

            var response = await next();

            await _idempotencyCache.SetAsync(scopedKey, response, cancellationToken);

            return response;
        }
        finally
        {
            semaphore.Release();
            _locks.TryRemove(scopedKey, out _);
        }
    }
}
