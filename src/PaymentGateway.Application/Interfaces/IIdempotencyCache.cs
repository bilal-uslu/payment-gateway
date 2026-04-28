namespace PaymentGateway.Application.Interfaces;

public interface IIdempotencyCache
{
    Task<(bool Found, TResponse? Response)> TryGetAsync<TResponse>(string idempotencyKey, CancellationToken cancellationToken = default);
    Task SetAsync<TResponse>(string idempotencyKey, TResponse response, CancellationToken cancellationToken = default);
}
