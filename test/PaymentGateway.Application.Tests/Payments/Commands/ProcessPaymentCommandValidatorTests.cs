using FluentAssertions;
using FluentValidation;
using PaymentGateway.Application.Payments.Commands.ProcessPayment;

namespace PaymentGateway.Application.Tests.Payments.Commands;

public class ProcessPaymentCommandValidatorTests
{
    private readonly ProcessPaymentCommandValidator _validator = new();

    private static ProcessPaymentCommand ValidCommand() => new()
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

    [Fact]
    public async Task ValidateAsync_WithValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]           // too short
    [InlineData("12345678901234567890")] // too long (20 chars)
    [InlineData("222240534324ABCD")]     // non-numeric
    public async Task ValidateAsync_WithInvalidCardNumber_FailsValidation(string cardNumber)
    {
        var cmd = ValidCommand();
        cmd.CardNumber = cardNumber;
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CardNumber");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public async Task ValidateAsync_WithInvalidExpiryMonth_FailsValidation(int month)
    {
        var cmd = ValidCommand();
        cmd.ExpiryMonth = month;
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiryMonth");
    }

    [Fact]
    public async Task ValidateAsync_WithExpiredDate_FailsValidation()
    {
        var cmd = ValidCommand();
        cmd.ExpiryMonth = 1;
        cmd.ExpiryYear = 2000;
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiryDate");
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("JPY")]
    public async Task ValidateAsync_WithInvalidCurrency_FailsValidation(string currency)
    {
        var cmd = ValidCommand();
        cmd.Currency = currency;
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public async Task ValidateAsync_WithSupportedCurrency_PassesValidation(string currency)
    {
        var cmd = ValidCommand();
        cmd.Currency = currency;
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_WithNonPositiveAmount_FailsValidation(long amount)
    {
        var cmd = ValidCommand();
        cmd.Amount = amount;
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]     // too short
    [InlineData("12345")]  // too long
    [InlineData("12A")]    // non-numeric
    public async Task ValidateAsync_WithInvalidCvv_FailsValidation(string cvv)
    {
        var cmd = ValidCommand();
        cmd.Cvv = cvv;
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cvv");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public async Task ValidateAsync_WithValidCvv_PassesValidation(string cvv)
    {
        var cmd = ValidCommand();
        cmd.Cvv = cvv;
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
