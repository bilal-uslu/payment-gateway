namespace PaymentGateway.Domain.Exceptions;

/// <summary>
/// Thrown when a card number fails domain validation.
/// </summary>
public class InvalidCardNumberException : DomainException
{
    public InvalidCardNumberException(string message) : base(message) { }
}
