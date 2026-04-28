using FluentAssertions;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests.Entities;

public class PaymentTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();

    private static CardDetails ValidCardDetails => CardDetails.Create(
        CardNumber.Create("1234567890123456"),
        ExpiryDate.Create(12, DateTime.UtcNow.Year + 1),
        CardVerificationValue.Create("123"));

    private static Money ValidMoney => Money.Create(1000, "USD");

    [Fact]
    public void Create_WithValidParameters_ShouldCreatePendingPayment()
    {
        var payment = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);

        payment.Id.Should().NotBeEmpty();
        payment.MerchantId.Should().Be(MerchantId);
        payment.CardDetails.Should().NotBeNull();
        payment.Money.Should().NotBeNull();
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.AuthorizationCode.Should().BeNull();
        payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullCardDetails_ShouldThrowArgumentNullException()
    {
        var act = () => Payment.Create(MerchantId, null!, ValidMoney);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullMoney_ShouldThrowArgumentNullException()
    {
        var act = () => Payment.Create(MerchantId, ValidCardDetails, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Authorize_WhenPending_ShouldSetStatusToAuthorizedAndSetAuthorizationCode()
    {
        var payment = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);

        payment.Authorize("AUTH-001");

        payment.Status.Should().Be(PaymentStatus.Authorized);
        payment.AuthorizationCode.Should().Be("AUTH-001");
    }

    [Fact]
    public void Authorize_WhenNotPending_ShouldThrowInvalidPaymentStateException()
    {
        var payment = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);
        payment.Authorize("AUTH-001");

        var act = () => payment.Authorize("AUTH-002");

        act.Should().Throw<InvalidPaymentStateException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Authorize_WithEmptyAuthorizationCode_ShouldThrowArgumentException(string authCode)
    {
        var payment = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);

        var act = () => payment.Authorize(authCode);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Authorization code is required*");
    }

    [Fact]
    public void Decline_WhenPending_ShouldSetStatusToDeclined()
    {
        var payment = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);

        payment.Decline();

        payment.Status.Should().Be(PaymentStatus.Declined);
        payment.AuthorizationCode.Should().BeNull();
    }

    [Fact]
    public void Decline_WhenNotPending_ShouldThrowInvalidPaymentStateException()
    {
        var payment = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);
        payment.Decline();

        var act = () => payment.Decline();

        act.Should().Throw<InvalidPaymentStateException>();
    }

    [Fact]
    public void Reject_WhenPending_ShouldSetStatusToRejected()
    {
        var payment = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);

        payment.Reject();

        payment.Status.Should().Be(PaymentStatus.Rejected);
        payment.AuthorizationCode.Should().BeNull();
    }

    [Fact]
    public void Reject_WhenNotPending_ShouldThrowInvalidPaymentStateException()
    {
        var payment = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);
        payment.Reject();

        var act = () => payment.Reject();

        act.Should().Throw<InvalidPaymentStateException>();
    }

    [Fact]
    public void Reconstitute_ShouldRestoreAllProperties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        const string authCode = "AUTH-XYZ";

        var payment = Payment.Reconstitute(id, MerchantId, ValidCardDetails, ValidMoney,
            PaymentStatus.Authorized, authCode, createdAt);

        payment.Id.Should().Be(id);
        payment.MerchantId.Should().Be(MerchantId);
        payment.Status.Should().Be(PaymentStatus.Authorized);
        payment.AuthorizationCode.Should().Be(authCode);
        payment.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Create_EachCallShouldProduceUniqueId()
    {
        var payment1 = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);
        var payment2 = Payment.Create(MerchantId, ValidCardDetails, ValidMoney);

        payment1.Id.Should().NotBe(payment2.Id);
    }
}
