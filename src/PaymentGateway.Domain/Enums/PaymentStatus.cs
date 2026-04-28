namespace PaymentGateway.Domain.Enums;

/// <summary>
/// Represents the status of a payment transaction.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// The payment is pending processing.
    /// </summary>
    Pending,

    /// <summary>
    /// The payment was authorized by the acquiring bank.
    /// </summary>
    Authorized,

    /// <summary>
    /// The payment was declined by the acquiring bank.
    /// </summary>
    Declined,

    /// <summary>
    /// The payment was rejected by the payment gateway due to invalid information.
    /// No call was made to the acquiring bank.
    /// </summary>
    Rejected
}
