namespace PaymentGateway.Domain.Exceptions;

/// <summary>
/// Thrown when a card expiry date fails domain validation.
/// </summary>
public class InvalidExpiryDateException : DomainException
{
    public InvalidExpiryDateException(string message) : base(message) { }
}
