namespace PaymentGateway.Domain.Exceptions;

/// <summary>
/// Base class for all domain exceptions.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}
