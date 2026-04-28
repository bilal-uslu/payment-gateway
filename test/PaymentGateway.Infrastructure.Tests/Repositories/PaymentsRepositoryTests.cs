using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.ValueObjects;
using PaymentGateway.Infrastructure.Repositories;

namespace PaymentGateway.Infrastructure.Tests.Repositories;

public class PaymentsRepositoryTests
{
    private readonly PaymentsRepository _sut = new(new NullLogger<PaymentsRepository>());

    private static Payment CreatePayment(Guid? merchantId = null)
    {
        var cardNumber = CardNumber.Create("2222405343248877");
        var expiryDate = ExpiryDate.Create(12, DateTime.UtcNow.Year + 1);
        var cvv = CardVerificationValue.Create("123");
        var cardDetails = CardDetails.Create(cardNumber, expiryDate, cvv);
        var money = Money.Create(100, "GBP");
        return Payment.Create(merchantId ?? Guid.NewGuid(), cardDetails, money);
    }

    [Fact]
    public async Task AddAsync_StoresPayment_CanBeRetrievedById()
    {
        var payment = CreatePayment();

        await _sut.AddAsync(payment);
        var retrieved = await _sut.GetByIdAsync(payment.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPaymentDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenPaymentExists_ReturnsCorrectPayment()
    {
        var payment = CreatePayment();
        await _sut.AddAsync(payment);

        var result = await _sut.GetByIdAsync(payment.Id);

        result.Should().NotBeNull();
        result!.MerchantId.Should().Be(payment.MerchantId);
        result.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var results = await _sut.GetAllAsync();

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllStoredPayments()
    {
        var payment1 = CreatePayment();
        var payment2 = CreatePayment();
        await _sut.AddAsync(payment1);
        await _sut.AddAsync(payment2);

        var results = await _sut.GetAllAsync();

        results.Should().HaveCount(2);
        results.Select(p => p.Id).Should().Contain([payment1.Id, payment2.Id]);
    }

    [Fact]
    public async Task AddAsync_WhenPaymentAddedTwice_OverwritesExistingEntry()
    {
        var payment = CreatePayment();
        await _sut.AddAsync(payment);
        payment.Authorize("AUTH-001");
        await _sut.AddAsync(payment);

        var results = await _sut.GetAllAsync();

        results.Should().HaveCount(1);
        results.First().Status.Should().Be(PaymentStatus.Authorized);
    }
}
