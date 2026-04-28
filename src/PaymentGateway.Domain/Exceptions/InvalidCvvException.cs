namespace PaymentGateway.Domain.Exceptions;

/// <summary>
/// Thrown when a card verification value (CVV) fails domain validation.
/// </summary>
public class InvalidCvvException : DomainException
{
    public InvalidCvvException(string message) : base(message) { }
}
