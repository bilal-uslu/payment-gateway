namespace PaymentGateway.Domain.ValueObjects;

/// <summary>
/// Represents the details of a payment card.
/// </summary>
public class CardDetails
{
    /// <summary>
    /// Gets the card number (14-19 digits).
    /// </summary>
    public CardNumber CardNumber { get; private set; }

    /// <summary>
    /// Gets the card expiry date.
    /// </summary>
    public ExpiryDate ExpiryDate { get; private set; }

    /// <summary>
    /// Gets the card verification value (CVV/CVC).
    /// </summary>
    public CardVerificationValue Cvv { get; private set; }

    /// <summary>
    /// Creates a new instance of the <see cref="CardDetails"/> class.
    /// </summary>
    /// <param name="cardNumber">The card number (14-19 numeric digits).</param>
    /// <param name="expiryDate">The card expiry date.</param>
    /// <param name="cvv">The card verification value.</param>
    public static CardDetails Create(CardNumber cardNumber, ExpiryDate expiryDate, CardVerificationValue cvv)
        => new(cardNumber, expiryDate, cvv);

    private CardDetails(CardNumber cardNumber, ExpiryDate expiryDate, CardVerificationValue cvv)
    {
        ArgumentNullException.ThrowIfNull(cardNumber);
        ArgumentNullException.ThrowIfNull(expiryDate);
        ArgumentNullException.ThrowIfNull(cvv);

        CardNumber = cardNumber;
        ExpiryDate = expiryDate;
        Cvv = cvv;
    }

    public string GetLastFourDigits()
    {
        return CardNumber.GetLastFourDigits();
    }

    /// <summary>
    /// Gets a masked version of the card number where all digits except the last four are replaced with asterisks.
    /// </summary>
    /// <returns>A masked card number (e.g., "************1234").</returns>
    public string GetMaskedCardNumber()
    {
        return CardNumber.GetMasked();
    }
}
