using FluentAssertions;

using PaymentGateway.Api.Mappers;
using PaymentGateway.Application.Payments.Queries.GetPayment;
using DomainPaymentStatus = PaymentGateway.Domain.Enums.PaymentStatus;
using ApiPaymentStatus = PaymentGateway.Api.Enums.PaymentStatus;

namespace PaymentGateway.Api.Tests.Mappers;

public class GetPaymentMapperTests
{
    [Fact]
    public void ToResponse_MapsAllFieldsCorrectly()
    {
        var paymentId = Guid.NewGuid();
        var result = new GetPaymentResult
        {
            Id = paymentId,
            Status = DomainPaymentStatus.Authorized,
            CardNumberLastFour = "4242",
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Currency = "USD",
            Amount = 500
        };

        var response = result.ToResponse();

        response.Id.Should().Be(paymentId);
        response.Status.Should().Be(ApiPaymentStatus.Authorized);
        response.CardNumberLastFour.Should().Be("4242");
        response.ExpiryMonth.Should().Be(12);
        response.ExpiryYear.Should().Be(2030);
        response.Currency.Should().Be("USD");
        response.Amount.Should().Be(500);
    }

    [Theory]
    [InlineData(DomainPaymentStatus.Authorized, ApiPaymentStatus.Authorized)]
    [InlineData(DomainPaymentStatus.Declined, ApiPaymentStatus.Declined)]
    [InlineData(DomainPaymentStatus.Rejected, ApiPaymentStatus.Rejected)]
    public void ToResponse_MapsStatusCorrectly(DomainPaymentStatus domainStatus, ApiPaymentStatus expectedApiStatus)
    {
        var result = new GetPaymentResult
        {
            Id = Guid.NewGuid(),
            Status = domainStatus,
            CardNumberLastFour = "1234",
            ExpiryMonth = 1,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100
        };

        var response = result.ToResponse();

        response.Status.Should().Be(expectedApiStatus);
    }
}
