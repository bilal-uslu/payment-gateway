using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Domain.ValueObjects;

/// <summary>
/// Represents a payment card number.
/// </summary>
public class CardNumber
{
    /// <summary>
    /// Gets the card number value (14-19 numeric digits).
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="CardNumber"/> class.
    /// </summary>
    /// <param name="value">The card number (14-19 numeric digits).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the card number is missing, not 14-19 characters, or contains non-numeric characters.
    /// </exception>
    public static CardNumber Create(string value) => new(value);

    private CardNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidCardNumberException("Card number is required");

        if (value.Length < 14 || value.Length > 19)
            throw new InvalidCardNumberException("Card number must be between 14-19 characters long");

        if (!value.All(char.IsDigit))
            throw new InvalidCardNumberException("Card number must only contain numeric characters");

        Value = value;
    }

    /// <summary>
    /// Gets the first six digits of the card number (Bank Identification Number).
    /// </summary>
    public string GetBin() => Value[..6];

    /// <summary>
    /// Gets the last four digits of the card number.
    /// </summary>
    public string GetLastFourDigits()
    {
        return Value.Length >= 4
            ? Value[^4..]
            : Value;
    }

    /// <summary>
    /// Gets a masked version of the card number where all digits except the last four are replaced with asterisks.
    /// </summary>
    /// <returns>A masked card number (e.g., "************1234").</returns>
    public string GetMasked()
    {
        if (Value.Length <= 4)
            return new string('*', Value.Length);

        return new string('*', Value.Length - 4) + GetLastFourDigits();
    }

    public override bool Equals(object? obj) => obj is CardNumber other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
