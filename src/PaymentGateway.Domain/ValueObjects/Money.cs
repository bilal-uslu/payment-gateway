using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with an amount and currency.
/// </summary>
public class Money
{
    /// <summary>
    /// The list of supported ISO currency codes.
    /// </summary>
    private static readonly HashSet<string> SupportedCurrencies = new() { "USD", "GBP", "EUR" };

    /// <summary>
    /// Gets the amount in the minor currency unit (e.g., cents for USD).
    /// </summary>
    public long Amount { get; private set; }

    /// <summary>
    /// Gets the three-letter ISO 4217 currency code.
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="Money"/> class.
    /// </summary>
    /// <param name="amount">The amount in the minor currency unit. Must be a positive integer.</param>
    /// <param name="currency">The three-letter ISO 4217 currency code (e.g., USD, GBP, EUR).</param>
    /// <exception cref="ArgumentException">Thrown when amount is negative, currency is invalid, or currency is not supported.</exception>
    public static Money Create(long amount, string currency) => new(amount, currency);

    private Money(long amount, string currency)
    {
        if (amount < 0)
            throw new InvalidMoneyException("Amount must be a positive integer");

        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidMoneyException("Currency is required");

        if (currency.Length != 3)
            throw new InvalidMoneyException("Currency must be 3 characters");

        var upperCurrency = currency.ToUpperInvariant();
        if (!SupportedCurrencies.Contains(upperCurrency))
            throw new InvalidMoneyException($"Currency '{currency}' is not supported");

        Amount = amount;
        Currency = upperCurrency;
    }
}
