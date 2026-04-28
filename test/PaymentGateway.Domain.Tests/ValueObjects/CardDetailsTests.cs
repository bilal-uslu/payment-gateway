using FluentAssertions;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests.ValueObjects;

public class CardDetailsTests
{
    private static CardNumber ValidCardNumber => CardNumber.Create("1234567890123456");
    private static ExpiryDate ValidExpiryDate => ExpiryDate.Create(12, DateTime.UtcNow.Year + 1);
    private static CardVerificationValue ValidCvv => CardVerificationValue.Create("123");

    [Fact]
    public void Create_WithValidDetails_ShouldSucceed()
    {
        var cardDetails = CardDetails.Create(ValidCardNumber, ValidExpiryDate, ValidCvv);

        cardDetails.CardNumber.Should().Be(ValidCardNumber);
        cardDetails.ExpiryDate.Should().Be(ValidExpiryDate);
        cardDetails.Cvv.Should().Be(ValidCvv);
    }

    [Fact]
    public void Create_WithNullCardNumber_ShouldThrowArgumentNullException()
    {
        var act = () => CardDetails.Create(null!, ValidExpiryDate, ValidCvv);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullExpiryDate_ShouldThrowArgumentNullException()
    {
        var act = () => CardDetails.Create(ValidCardNumber, null!, ValidCvv);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithNullCvv_ShouldThrowArgumentNullException()
    {
        var act = () => CardDetails.Create(ValidCardNumber, ValidExpiryDate, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetLastFourDigits_ShouldReturnLastFourDigitsOfCardNumber()
    {
        var cardDetails = CardDetails.Create(ValidCardNumber, ValidExpiryDate, ValidCvv);

        cardDetails.GetLastFourDigits().Should().Be("3456");
    }

    [Fact]
    public void GetMaskedCardNumber_ShouldReturnMaskedCardNumber()
    {
        var cardDetails = CardDetails.Create(ValidCardNumber, ValidExpiryDate, ValidCvv);

        cardDetails.GetMaskedCardNumber().Should().Be("************3456");
    }
}
