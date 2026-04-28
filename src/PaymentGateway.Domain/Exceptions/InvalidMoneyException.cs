namespace PaymentGateway.Domain.Exceptions;

/// <summary>
/// Thrown when a monetary value fails domain validation.
/// </summary>
public class InvalidMoneyException : DomainException
{
    public InvalidMoneyException(string message) : base(message) { }
}
