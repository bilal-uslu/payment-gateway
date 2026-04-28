using MediatR;
using Microsoft.Extensions.Logging;
using PaymentGateway.Domain.Repositories;

namespace PaymentGateway.Application.Payments.Queries.GetPayment;

public class GetPaymentQueryHandler(
    IPaymentsRepository paymentsRepository,
    ILogger<GetPaymentQueryHandler> logger) : IRequestHandler<GetPaymentQuery, GetPaymentResult?>
{
    public async Task<GetPaymentResult?> Handle(GetPaymentQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Retrieving payment {PaymentId} for MerchantId {MerchantId}",
            request.Id, request.MerchantId);

        var payment = await paymentsRepository.GetByIdAsync(request.Id, cancellationToken);

        if (payment is null || payment.MerchantId != request.MerchantId)
        {
            logger.LogWarning(
                "Payment {PaymentId} not found or does not belong to MerchantId {MerchantId}",
                request.Id, request.MerchantId);
            return null;
        }

        return new GetPaymentResult
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardDetails.GetLastFourDigits(),
            ExpiryMonth = payment.CardDetails.ExpiryDate.Month,
            ExpiryYear = payment.CardDetails.ExpiryDate.Year,
            Currency = payment.Money.Currency,
            Amount = payment.Money.Amount
        };
    }
}
