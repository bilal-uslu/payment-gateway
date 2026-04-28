using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Repositories;

namespace PaymentGateway.Infrastructure.Repositories;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();
    private readonly ILogger<PaymentsRepository> _logger;

    public PaymentsRepository(ILogger<PaymentsRepository> logger)
    {
        _logger = logger;
    }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _payments[payment.Id] = payment;

        _logger.LogInformation(
            "Payment {PaymentId} added to repository for MerchantId {MerchantId} with status {PaymentStatus}, CardNumberLastFour={CardNumberLastFour}, ExpiryDate={ExpiryMonth}/{ExpiryYear}, Currency={Currency}, Amount={Amount}",
            payment.Id,
            payment.MerchantId,
            payment.Status,
            payment.CardDetails.CardNumber.Value[^4..],
            payment.CardDetails.ExpiryDate.Month,
            payment.CardDetails.ExpiryDate.Year,
            payment.Money.Currency,
            payment.Money.Amount);

        return Task.CompletedTask;
    }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _payments.TryGetValue(id, out Payment? payment);

        if (payment is null)
            _logger.LogWarning("Payment {PaymentId} was not found in the repository", id);
        else
            _logger.LogInformation("Payment {PaymentId} retrieved from repository", id);

        return Task.FromResult(payment);
    }

    public Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Payment>>(_payments.Values.ToList().AsReadOnly());
    }
}
