using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Payments.Queries.GetPayment;

namespace PaymentGateway.Api.Mappers;

public static class GetPaymentMapper
{
    public static GetPaymentResponse ToResponse(this GetPaymentResult result) =>
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
