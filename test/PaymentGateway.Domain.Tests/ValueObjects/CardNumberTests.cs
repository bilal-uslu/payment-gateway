using FluentAssertions;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests.ValueObjects;

public class CardNumberTests
{
    [Theory]
    [InlineData("12345678901234")]   // 14 digits
    [InlineData("1234567890123456")] // 16 digits
    [InlineData("1234567890123456789")] // 19 digits
    public void Create_WithValidCardNumber_ShouldSucceed(string value)
    {
        var cardNumber = CardNumber.Create(value);

        cardNumber.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithNullOrEmpty_ShouldThrowInvalidCardNumberException()
    {
        var act = () => CardNumber.Create("");

        act.Should().Throw<InvalidCardNumberException>()
            .WithMessage("Card number is required");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldThrowInvalidCardNumberException()
    {
        var act = () => CardNumber.Create("   ");

        act.Should().Throw<InvalidCardNumberException>()
            .WithMessage("Card number is required");
    }

    [Theory]
    [InlineData("1234567890123")]    // 13 digits (too short)
    [InlineData("12345678901234567890")] // 20 digits (too long)
    public void Create_WithInvalidLength_ShouldThrowInvalidCardNumberException(string value)
    {
        var act = () => CardNumber.Create(value);

        act.Should().Throw<InvalidCardNumberException>()
            .WithMessage("Card number must be between 14-19 characters long");
    }

    [Theory]
    [InlineData("1234567890ABCD")]
    [InlineData("1234 5678 9012 3456")]
    [InlineData("1234-5678-9012-3456")]
    public void Create_WithNonNumericCharacters_ShouldThrowInvalidCardNumberException(string value)
    {
        var act = () => CardNumber.Create(value);

        act.Should().Throw<InvalidCardNumberException>()
            .WithMessage("Card number must only contain numeric characters");
    }

    [Fact]
    public void GetBin_ShouldReturnFirstSixDigits()
    {
        var cardNumber = CardNumber.Create("1234567890123456");

        cardNumber.GetBin().Should().Be("123456");
    }

    [Fact]
    public void GetLastFourDigits_ShouldReturnLastFourDigits()
    {
        var cardNumber = CardNumber.Create("1234567890123456");

        cardNumber.GetLastFourDigits().Should().Be("3456");
    }

    [Fact]
    public void GetMasked_ShouldMaskAllButLastFourDigits()
    {
        var cardNumber = CardNumber.Create("1234567890123456");

        cardNumber.GetMasked().Should().Be("************3456");
    }

    [Fact]
    public void GetMasked_WithFourteenDigitCard_ShouldMaskAllButLastFour()
    {
        var cardNumber = CardNumber.Create("12345678901234");

        cardNumber.GetMasked().Should().Be("**********1234");
    }
}
