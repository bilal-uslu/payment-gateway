using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaymentGateway.Application.Payments.Queries.GetPayment;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Repositories;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Tests.Payments.Queries;

public class GetPaymentQueryHandlerTests
{
    private readonly Mock<IPaymentsRepository> _repositoryMock = new();
    private readonly NullLogger<GetPaymentQueryHandler> _logger = new();

    private GetPaymentQueryHandler CreateHandler() => new(_repositoryMock.Object, _logger);

    private static Payment CreatePayment(Guid merchantId)
    {
        var cardDetails = CardDetails.Create(
            CardNumber.Create("2222405343248877"),
            ExpiryDate.Create(4, DateTime.UtcNow.Year + 1),
            CardVerificationValue.Create("123"));
        var money = Money.Create(100, "GBP");
        return Payment.Create(merchantId, cardDetails, money);
    }

    [Fact]
    public async Task Handle_WhenPaymentExists_ReturnsGetPaymentResult()
    {
        var merchantId = Guid.NewGuid();
        var payment = CreatePayment(merchantId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPaymentQuery { Id = payment.Id, MerchantId = merchantId }, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(payment.Id);
        result.Status.Should().Be(PaymentStatus.Pending);
        result.CardNumberLastFour.Should().Be("8877");
        result.Currency.Should().Be("GBP");
        result.Amount.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WhenPaymentDoesNotExist_ReturnsNull()
    {
        var paymentId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(paymentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPaymentQuery { Id = paymentId, MerchantId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPaymentBelongsToDifferentMerchant_ReturnsNull()
    {
        var merchantId = Guid.NewGuid();
        var payment = CreatePayment(merchantId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var differentMerchantId = Guid.NewGuid();
        var handler = CreateHandler();
        var result = await handler.Handle(new GetPaymentQuery { Id = payment.Id, MerchantId = differentMerchantId }, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPaymentExists_ReturnsCorrectExpiryDetails()
    {
        var merchantId = Guid.NewGuid();
        var payment = CreatePayment(merchantId);
        var futureYear = DateTime.UtcNow.Year + 1;

        _repositoryMock
            .Setup(r => r.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPaymentQuery { Id = payment.Id, MerchantId = merchantId }, CancellationToken.None);

        result!.ExpiryMonth.Should().Be(4);
        result.ExpiryYear.Should().Be(futureYear);
    }
}
