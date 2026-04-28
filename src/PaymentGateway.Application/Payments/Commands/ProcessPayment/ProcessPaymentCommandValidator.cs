using FluentValidation;

namespace PaymentGateway.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    private static readonly HashSet<string> AllowedCurrencies = new() { "USD", "EUR", "GBP" };

    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .WithMessage("Card number is required")
            .Length(14, 19)
            .WithMessage("Card number must be between 14 and 19 characters")
            .Matches(@"^\d+$")
            .WithMessage("Card number must only contain numeric characters");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("Expiry month must be between 1 and 12");

        RuleFor(x => x.ExpiryYear)
            .GreaterThan(0)
            .WithMessage("Expiry year is required");

        RuleFor(x => x)
            .Must(x => IsExpiryDateInFuture(x.ExpiryMonth, x.ExpiryYear))
            .WithMessage("Expiry date must be in the future")
            .WithName("ExpiryDate");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be 3 characters")
            .Must(currency => AllowedCurrencies.Contains(currency))
            .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Cvv)
            .NotEmpty()
            .WithMessage("CVV is required")
            .Length(3, 4)
            .WithMessage("CVV must be 3 or 4 characters")
            .Matches(@"^\d+$")
            .WithMessage("CVV must only contain numeric characters");
    }

    private static bool IsExpiryDateInFuture(int month, int year)
    {
        if (month < 1 || month > 12 || year <= 0) return false;
        var now = DateTime.UtcNow;
        var expiryDate = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
        return expiryDate >= now;
    }
}
