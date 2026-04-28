using PaymentGateway.Domain.Exceptions;

namespace PaymentGateway.Domain.ValueObjects;

/// <summary>
/// Represents a card expiry date.
/// </summary>
public class ExpiryDate
{
    /// <summary>
    /// Gets the expiry month (1-12).
    /// </summary>
    public int Month { get; private set; }

    /// <summary>
    /// Gets the expiry year.
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="ExpiryDate"/> class.
    /// </summary>
    /// <param name="month">The expiry month (1-12).</param>
    /// <param name="year">The expiry year (must not be in the past).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the expiry month is invalid or the card has expired.
    /// </exception>
    public static ExpiryDate Create(int month, int year) => new(month, year);

    private ExpiryDate(int month, int year)
    {
        if (month < 1 || month > 12)
            throw new InvalidExpiryDateException("Expiry month must be between 1-12");

        if (year < DateTime.UtcNow.Year ||
            (year == DateTime.UtcNow.Year && month < DateTime.UtcNow.Month))
            throw new InvalidExpiryDateException("Card has expired");

        Month = month;
        Year = year;
    }

    public override bool Equals(object? obj) => obj is ExpiryDate other && Month == other.Month && Year == other.Year;
    public override int GetHashCode() => HashCode.Combine(Month, Year);
}
