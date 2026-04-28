namespace PaymentGateway.Application.Interfaces;

/// <summary>
/// Marker interface for MediatR requests that support idempotent processing.
/// Requests implementing this interface will be deduplicated by the
/// <see cref="Behaviors.IdempotencyBehavior{TRequest,TResponse}"/> pipeline.
/// The cache key is scoped per merchant by combining <see cref="MerchantId"/>
/// and <see cref="IdempotencyKey"/>.
/// </summary>
public interface IIdempotentRequest<TResponse>
{
    Guid MerchantId { get; }
    string IdempotencyKey { get; }
}
