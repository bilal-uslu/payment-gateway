using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Payments.Commands.ProcessPayment;

namespace PaymentGateway.Api.Mappers;

public static class PostPaymentMapper
{
    public static ProcessPaymentCommand ToCommand(this PostPaymentRequest request, Guid merchantId, string idempotencyKey) =>
        new()
        {
            MerchantId = merchantId,
            CardNumber = request.CardNumber,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv,
            IdempotencyKey = idempotencyKey
        };

    public static PostPaymentResponse ToResponse(this ProcessPaymentResult result) =>
        new()
        {
            Id = result.Id,
            Status = result.Status.ToApiStatus(),
            CardNumberLastFour = result.CardNumberLastFour,
            ExpiryMonth = result.ExpiryMonth,
            ExpiryYear = result.ExpiryYear,
            Currency = result.Currency,
            Amount = result.Amount
        };
}
