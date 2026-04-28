using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Models;
using PaymentGateway.Application.Payments.Commands.ProcessPayment;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Repositories;

namespace PaymentGateway.Application.Tests.Payments.Commands;

public class ProcessPaymentCommandHandlerTests
{
    private readonly Mock<IAcquiringBankClient> _bankClientMock = new();
    private readonly Mock<IPaymentsRepository> _repositoryMock = new();
    private readonly NullLogger<ProcessPaymentCommandHandler> _logger = new();

    private static readonly ProcessPaymentCommand ValidCommand = new()
    {
        MerchantId = Guid.NewGuid(),
        CardNumber = "2222405343248877",
        ExpiryMonth = 4,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123",
        IdempotencyKey = Guid.NewGuid().ToString()
    };

    private ProcessPaymentCommandHandler CreateHandler(IEnumerable<IPaymentBusinessRule>? rules = null)
        => new(_bankClientMock.Object, _repositoryMock.Object, rules ?? [], _logger);

    [Fact]
    public async Task Handle_WhenBankAuthorizesPayment_ReturnsAuthorizedResult()
    {
        _bankClientMock
            .Setup(b => b.ProcessPaymentAsync(It.IsAny<AcquiringBankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcquiringBankResponse { Authorized = true, AuthorizationCode = "AUTH123" });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Authorized);
        result.CardNumberLastFour.Should().Be("8877");
        result.Currency.Should().Be("GBP");
        result.Amount.Should().Be(100);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Payment>(p => p.Status == PaymentStatus.Authorized), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBankDeclinesPayment_ReturnsDeclinedResult()
    {
        _bankClientMock
            .Setup(b => b.ProcessPaymentAsync(It.IsAny<AcquiringBankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcquiringBankResponse { Authorized = false, AuthorizationCode = null });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Declined);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Payment>(p => p.Status == PaymentStatus.Declined), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBankReturnsAuthorizedFalseWithCode_ReturnsDeclinedResult()
    {
        _bankClientMock
            .Setup(b => b.ProcessPaymentAsync(It.IsAny<AcquiringBankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcquiringBankResponse { Authorized = false, AuthorizationCode = "SHOULD_IGNORE" });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Declined);
    }

    [Fact]
    public async Task Handle_WhenBusinessRuleIsViolated_ReturnsRejectedResultWithoutCallingBank()
    {
        var ruleMock = new Mock<IPaymentBusinessRule>();
        ruleMock.Setup(r => r.IsViolatedBy(It.IsAny<Payment>())).Returns(true);

        var handler = CreateHandler([ruleMock.Object]);
        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Rejected);
        _bankClientMock.Verify(b => b.ProcessPaymentAsync(It.IsAny<AcquiringBankRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Payment>(p => p.Status == PaymentStatus.Rejected), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFirstRulePassesAndSecondViolates_RejectsPayment()
    {
        var passingRule = new Mock<IPaymentBusinessRule>();
        passingRule.Setup(r => r.IsViolatedBy(It.IsAny<Payment>())).Returns(false);

        var blockingRule = new Mock<IPaymentBusinessRule>();
        blockingRule.Setup(r => r.IsViolatedBy(It.IsAny<Payment>())).Returns(true);

        var handler = CreateHandler([passingRule.Object, blockingRule.Object]);
        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Rejected);
        _bankClientMock.Verify(b => b.ProcessPaymentAsync(It.IsAny<AcquiringBankRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ResultContainsCorrectCardLastFour()
    {
        _bankClientMock
            .Setup(b => b.ProcessPaymentAsync(It.IsAny<AcquiringBankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcquiringBankResponse { Authorized = true, AuthorizationCode = "X" });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.CardNumberLastFour.Should().Be("8877");
    }

    [Fact]
    public async Task Handle_ResultContainsCorrectExpiryDetails()
    {
        _bankClientMock
            .Setup(b => b.ProcessPaymentAsync(It.IsAny<AcquiringBankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcquiringBankResponse { Authorized = true, AuthorizationCode = "X" });

        var handler = CreateHandler();
        var result = await handler.Handle(ValidCommand, CancellationToken.None);

        result.ExpiryMonth.Should().Be(ValidCommand.ExpiryMonth);
        result.ExpiryYear.Should().Be(ValidCommand.ExpiryYear);
    }
}
