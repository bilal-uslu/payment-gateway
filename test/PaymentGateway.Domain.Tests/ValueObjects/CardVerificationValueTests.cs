using FluentAssertions;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests.ValueObjects;

public class CardVerificationValueTests
{
    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void Create_WithValidCvv_ShouldSucceed(string value)
    {
        var cvv = CardVerificationValue.Create(value);

        cvv.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrowInvalidCvvException()
    {
        var act = () => CardVerificationValue.Create("");

        act.Should().Throw<InvalidCvvException>()
            .WithMessage("CVV is required");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldThrowInvalidCvvException()
    {
        var act = () => CardVerificationValue.Create("   ");

        act.Should().Throw<InvalidCvvException>()
            .WithMessage("CVV is required");
    }

    [Theory]
    [InlineData("12")]   // too short
    [InlineData("12345")] // too long
    public void Create_WithInvalidLength_ShouldThrowInvalidCvvException(string value)
    {
        var act = () => CardVerificationValue.Create(value);

        act.Should().Throw<InvalidCvvException>()
            .WithMessage("CVV must be 3-4 characters long");
    }

    [Theory]
    [InlineData("12A")]
    [InlineData("AB34")]
    [InlineData("1 3")]
    public void Create_WithNonNumericCharacters_ShouldThrowInvalidCvvException(string value)
    {
        var act = () => CardVerificationValue.Create(value);

        act.Should().Throw<InvalidCvvException>()
            .WithMessage("CVV must only contain numeric characters");
    }
}
