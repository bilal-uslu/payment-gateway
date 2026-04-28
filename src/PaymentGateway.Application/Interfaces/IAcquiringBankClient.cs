using PaymentGateway.Application.Models;

namespace PaymentGateway.Application.Interfaces;

public interface IAcquiringBankClient
{
    Task<AcquiringBankResponse> ProcessPaymentAsync(AcquiringBankRequest request, CancellationToken cancellationToken = default);
}
