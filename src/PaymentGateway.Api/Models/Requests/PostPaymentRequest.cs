namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    /// <summary>The full card number (14–19 digits).</summary>
    /// <example>"2222405343248877"</example>
    public required string CardNumber { get; set; }

    /// <summary>The month the card expires (1–12).</summary>
    /// <example>12</example>
    public int ExpiryMonth { get; set; }

    /// <summary>The four-digit year the card expires.</summary>
    /// <example>2027</example>
    public int ExpiryYear { get; set; }

    /// <summary>The three-letter ISO 4217 currency code for the transaction.</summary>
    /// <example>USD</example>
    public required string Currency { get; set; }

    /// <summary>The payment amount in the smallest currency unit (e.g. cents).</summary>
    /// <example>1000</example>
    public long Amount { get; set; }

    /// <summary>The card verification value (CVV/CVC), typically 3 or 4 digits.</summary>
    /// <example>"123"</example>
    public required string Cvv { get; set; }
}