using FluentAssertions;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Theory]
    [InlineData(100, "USD")]
    [InlineData(0, "GBP")]
    [InlineData(9999, "EUR")]
    public void Create_WithValidAmountAndCurrency_ShouldSucceed(long amount, string currency)
    {
        var money = Money.Create(amount, currency);

        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }

    [Fact]
    public void Create_WithLowercaseCurrency_ShouldNormalizeToCurrencyUpperCase()
    {
        var money = Money.Create(100, "usd");

        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrowInvalidMoneyException()
    {
        var act = () => Money.Create(-1, "USD");

        act.Should().Throw<InvalidMoneyException>()
            .WithMessage("Amount must be a positive integer");
    }

    [Fact]
    public void Create_WithEmptyCurrency_ShouldThrowInvalidMoneyException()
    {
        var act = () => Money.Create(100, "");

        act.Should().Throw<InvalidMoneyException>()
            .WithMessage("Currency is required");
    }

    [Fact]
    public void Create_WithWhitespaceCurrency_ShouldThrowInvalidMoneyException()
    {
        var act = () => Money.Create(100, "   ");

        act.Should().Throw<InvalidMoneyException>()
            .WithMessage("Currency is required");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Create_WithWrongLengthCurrency_ShouldThrowInvalidMoneyException(string currency)
    {
        var act = () => Money.Create(100, currency);

        act.Should().Throw<InvalidMoneyException>()
            .WithMessage("Currency must be 3 characters");
    }

    [Fact]
    public void Create_WithUnsupportedCurrency_ShouldThrowInvalidMoneyException()
    {
        var act = () => Money.Create(100, "JPY");

        act.Should().Throw<InvalidMoneyException>()
            .WithMessage("Currency 'JPY' is not supported");
    }
}
