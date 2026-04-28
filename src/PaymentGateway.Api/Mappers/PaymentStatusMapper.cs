using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Mappers;

public static class PaymentStatusMapper
{
    public static Enums.PaymentStatus ToApiStatus(this PaymentStatus status) =>
        status switch
        {
            PaymentStatus.Authorized => Enums.PaymentStatus.Authorized,
            PaymentStatus.Declined => Enums.PaymentStatus.Declined,
            PaymentStatus.Rejected => Enums.PaymentStatus.Rejected,
            _ => throw new InvalidOperationException($"Unexpected payment status: {status}")
        };
}
