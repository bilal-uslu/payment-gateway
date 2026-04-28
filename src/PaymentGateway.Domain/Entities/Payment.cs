using PaymentGateway.Domain.Enums;
using PaymentGateway.Domain.Exceptions;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Domain.Entities;

public class Payment
{
    /// <summary>The unique identifier for the payment.</summary>
    public Guid Id { get; private set; }
    /// <summary>The unique identifier of the merchant who initiated the payment.</summary>
    public Guid MerchantId { get; private set; }
    /// <summary>The card details associated with the payment.</summary>
    public CardDetails CardDetails { get; private set; }
    /// <summary>The monetary amount and currency for the payment.</summary>
    public Money Money { get; private set; }
    /// <summary>The current status of the payment.</summary>
    public PaymentStatus Status { get; private set; }
    /// <summary>The authorization code received from the acquiring bank, or <see langword="null"/> if not yet authorized.</summary>
    public string? AuthorizationCode { get; private set; }
    /// <summary>The UTC date and time when the payment was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Creates a new <see cref="Payment"/> for processing.
    /// </summary>
    /// <param name="cardDetails">The card details for the payment.</param>
    /// <param name="money">The monetary amount and currency for the payment.</param>
    /// <exception cref="ArgumentNullException">Thrown when cardDetails or money is null.</exception>
    public static Payment Create(Guid merchantId, CardDetails cardDetails, Money money)
        => new(Guid.NewGuid(), merchantId, cardDetails, money, PaymentStatus.Pending, null, DateTime.UtcNow);

    /// Reconstitutes a <see cref="Payment"/> from persisted storage.
    /// </summary>
    /// <param name="id">The unique identifier for the payment.</param>
    /// <param name="merchantId">The unique identifier of the merchant.</param>
    /// <param name="cardDetails">The card details for the payment.</param>
    /// <param name="money">The monetary amount and currency for the payment.</param>
    /// <param name="status">The current status of the payment.</param>
    /// <param name="authorizationCode">The authorization code from the acquiring bank (optional).</param>
    /// <param name="createdAt">The UTC date and time when the payment was created.</param>
    /// <exception cref="ArgumentNullException">Thrown when cardDetails or money is null.</exception>
    public static Payment Reconstitute(Guid id, Guid merchantId, CardDetails cardDetails, Money money, PaymentStatus status, string? authorizationCode, DateTime createdAt)
        => new(id, merchantId, cardDetails, money, status, authorizationCode, createdAt);

    private Payment(Guid id, Guid merchantId, CardDetails cardDetails, Money money, PaymentStatus status, string? authorizationCode, DateTime createdAt)
    {
        Id = id;
        MerchantId = merchantId;
        CardDetails = cardDetails ?? throw new ArgumentNullException(nameof(cardDetails));
        Money = money ?? throw new ArgumentNullException(nameof(money));
        Status = status;
        AuthorizationCode = authorizationCode;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Authorizes the payment with the provided authorization code from the acquiring bank.
    /// </summary>
    /// <param name="authorizationCode">The authorization code received from the acquiring bank.</param>
    /// <exception cref="ArgumentException">Thrown when the authorization code is null or whitespace.</exception>
    public void Authorize(string authorizationCode)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidPaymentStateException("authorize", Status);

        if (string.IsNullOrWhiteSpace(authorizationCode))
            throw new ArgumentException("Authorization code is required", nameof(authorizationCode));

        Status = PaymentStatus.Authorized;
        AuthorizationCode = authorizationCode;
    }

    /// <summary>
    /// Marks the payment as declined.
    /// </summary>
    public void Decline()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidPaymentStateException("decline", Status);

        Status = PaymentStatus.Declined;
        AuthorizationCode = null;
    }

    /// <summary>
    /// Marks the payment as rejected due to validation failures.
    /// </summary>
    public void Reject()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidPaymentStateException("reject", Status);

        Status = PaymentStatus.Rejected;
        AuthorizationCode = null;
    }
}
