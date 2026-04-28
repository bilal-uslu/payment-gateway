using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Domain.ValueObjects;

/// <summary>
/// Represents a card verification value (CVV/CVC).
/// </summary>
public class CardVerificationValue
{
    /// <summary>
    /// Gets the CVV value.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="CardVerificationValue"/> class.
    /// </summary>
    /// <param name="value">The card verification value (3-4 numeric digits).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the CVV is missing, not 3-4 characters, or contains non-numeric characters.
    /// </exception>
    public static CardVerificationValue Create(string value) => new(value);

    private CardVerificationValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidCvvException("CVV is required");

        if (value.Length < 3 || value.Length > 4)
            throw new InvalidCvvException("CVV must be 3-4 characters long");

        if (!value.All(char.IsDigit))
            throw new InvalidCvvException("CVV must only contain numeric characters");

        Value = value;
    }

    public override bool Equals(object? obj) => obj is CardVerificationValue other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
