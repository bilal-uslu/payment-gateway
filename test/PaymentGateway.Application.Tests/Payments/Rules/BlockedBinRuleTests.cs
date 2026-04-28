using FluentAssertions;
using Microsoft.Extensions.Options;
using PaymentGateway.Application.Payments.Rules;
using PaymentGateway.Application.Settings;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Tests.Payments.Rules;

public class BlockedBinRuleTests
{
    private static BlockedBinRule CreateRule(params string[] blockedBins)
    {
        var settings = new PaymentRulesSettings { BlockedBins = blockedBins };
        return new BlockedBinRule(Options.Create(settings));
    }

    private static Payment CreatePaymentWithCard(string cardNumber)
    {
        var cardDetails = CardDetails.Create(
            CardNumber.Create(cardNumber),
            ExpiryDate.Create(4, DateTime.UtcNow.Year + 1),
            CardVerificationValue.Create("123"));
        var money = Money.Create(100, "GBP");
        return Payment.Create(Guid.NewGuid(), cardDetails, money);
    }

    [Fact]
    public void IsViolatedBy_WhenBinIsBlocked_ReturnsTrue()
    {
        var rule = CreateRule("222240");
        var payment = CreatePaymentWithCard("2222405343248877");

        rule.IsViolatedBy(payment).Should().BeTrue();
    }

    [Fact]
    public void IsViolatedBy_WhenBinIsNotBlocked_ReturnsFalse()
    {
        var rule = CreateRule("111111");
        var payment = CreatePaymentWithCard("2222405343248877");

        rule.IsViolatedBy(payment).Should().BeFalse();
    }

    [Fact]
    public void IsViolatedBy_WhenBlockedBinsIsEmpty_ReturnsFalse()
    {
        var rule = CreateRule();
        var payment = CreatePaymentWithCard("2222405343248877");

        rule.IsViolatedBy(payment).Should().BeFalse();
    }

    [Fact]
    public void IsViolatedBy_WhenMultipleBinsBlockedAndCardMatches_ReturnsTrue()
    {
        var rule = CreateRule("111111", "222240", "333333");
        var payment = CreatePaymentWithCard("2222405343248877");

        rule.IsViolatedBy(payment).Should().BeTrue();
    }

    [Fact]
    public void RejectionReason_IsNotEmpty()
    {
        var rule = CreateRule();
        rule.RejectionReason.Should().NotBeNullOrWhiteSpace();
    }
}
