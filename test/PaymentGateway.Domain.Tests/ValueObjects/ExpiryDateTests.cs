using FluentAssertions;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Tests.ValueObjects;

public class ExpiryDateTests
{
    private static readonly int CurrentYear = DateTime.UtcNow.Year;
    private static readonly int CurrentMonth = DateTime.UtcNow.Month;

    [Fact]
    public void Create_WithValidFutureDate_ShouldSucceed()
    {
        var futureYear = CurrentYear + 1;

        var expiryDate = ExpiryDate.Create(1, futureYear);

        expiryDate.Month.Should().Be(1);
        expiryDate.Year.Should().Be(futureYear);
    }

    [Fact]
    public void Create_WithCurrentMonthAndYear_ShouldSucceed()
    {
        var expiryDate = ExpiryDate.Create(CurrentMonth, CurrentYear);

        expiryDate.Month.Should().Be(CurrentMonth);
        expiryDate.Year.Should().Be(CurrentYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Create_WithInvalidMonth_ShouldThrowInvalidExpiryDateException(int month)
    {
        var act = () => ExpiryDate.Create(month, CurrentYear + 1);

        act.Should().Throw<InvalidExpiryDateException>()
            .WithMessage("Expiry month must be between 1-12");
    }

    [Fact]
    public void Create_WithPastYear_ShouldThrowInvalidExpiryDateException()
    {
        var act = () => ExpiryDate.Create(1, CurrentYear - 1);

        act.Should().Throw<InvalidExpiryDateException>()
            .WithMessage("Card has expired");
    }

    [Fact]
    public void Create_WithPastMonthInCurrentYear_ShouldThrowInvalidExpiryDateException()
    {
        if (CurrentMonth == 1)
            return; // Cannot test past month in current year when it's January

        var act = () => ExpiryDate.Create(CurrentMonth - 1, CurrentYear);

        act.Should().Throw<InvalidExpiryDateException>()
            .WithMessage("Card has expired");
    }
}
