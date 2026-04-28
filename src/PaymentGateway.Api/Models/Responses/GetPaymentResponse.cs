using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Models.Responses;

public class GetPaymentResponse
{
    /// <summary>The unique identifier of the payment.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>The current status of the payment (e.g. Authorized, Declined).</summary>
    /// <example>Authorized</example>
    public PaymentStatus Status { get; set; }

    /// <summary>The last four digits of the card used for this payment.</summary>
    /// <example>4242</example>
    public required string CardNumberLastFour { get; set; }

    /// <summary>The month the card expires (1–12).</summary>
    /// <example>12</example>
    public int ExpiryMonth { get; set; }

    /// <summary>The four-digit year the card expires.</summary>
    /// <example>2027</example>
    public int ExpiryYear { get; set; }

    /// <summary>The three-letter ISO 4217 currency code for the payment.</summary>
    /// <example>USD</example>
    public required string Currency { get; set; }

    /// <summary>The payment amount in the smallest currency unit (e.g. cents).</summary>
    /// <example>1000</example>
    public long Amount { get; set; }
}