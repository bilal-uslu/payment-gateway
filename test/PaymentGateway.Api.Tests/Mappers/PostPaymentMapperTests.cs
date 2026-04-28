using FluentAssertions;

using PaymentGateway.Api.Mappers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Application.Payments.Commands.ProcessPayment;
using DomainPaymentStatus = PaymentGateway.Domain.Enums.PaymentStatus;
using ApiPaymentStatus = PaymentGateway.Api.Enums.PaymentStatus;

namespace PaymentGateway.Api.Tests.Mappers;

public class PostPaymentMapperTests
{
    #region ToCommand

    [Fact]
    public void ToCommand_MapsAllFieldsCorrectly()
    {
        var merchantId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 4,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        var command = request.ToCommand(merchantId, idempotencyKey);

        command.MerchantId.Should().Be(merchantId);
        command.IdempotencyKey.Should().Be(idempotencyKey);
        command.CardNumber.Should().Be(request.CardNumber);
        command.ExpiryMonth.Should().Be(request.ExpiryMonth);
        command.ExpiryYear.Should().Be(request.ExpiryYear);
        command.Currency.Should().Be(request.Currency);
        command.Amount.Should().Be(request.Amount);
        command.Cvv.Should().Be(request.Cvv);
    }

    #endregion

    #region ToResponse

    [Fact]
    public void ToResponse_MapsAllFieldsCorrectly()
    {
        var paymentId = Guid.NewGuid();
        var result = new ProcessPaymentResult
        {
            Id = paymentId,
            Status = DomainPaymentStatus.Authorized,
            CardNumberLastFour = "8877",
            ExpiryMonth = 4,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100
        };

        var response = result.ToResponse();

        response.Id.Should().Be(paymentId);
        response.Status.Should().Be(ApiPaymentStatus.Authorized);
        response.CardNumberLastFour.Should().Be("8877");
        response.ExpiryMonth.Should().Be(4);
        response.ExpiryYear.Should().Be(2030);
        response.Currency.Should().Be("GBP");
        response.Amount.Should().Be(100);
    }

    [Theory]
    [InlineData(DomainPaymentStatus.Authorized, ApiPaymentStatus.Authorized)]
    [InlineData(DomainPaymentStatus.Declined, ApiPaymentStatus.Declined)]
    [InlineData(DomainPaymentStatus.Rejected, ApiPaymentStatus.Rejected)]
    public void ToResponse_MapsStatusCorrectly(DomainPaymentStatus domainStatus, ApiPaymentStatus expectedApiStatus)
    {
        var result = new ProcessPaymentResult
        {
            Id = Guid.NewGuid(),
            Status = domainStatus,
            CardNumberLastFour = "1234",
            ExpiryMonth = 1,
            ExpiryYear = 2030,
            Currency = "USD",
            Amount = 200
        };

        var response = result.ToResponse();

        response.Status.Should().Be(expectedApiStatus);
    }

    #endregion
}
