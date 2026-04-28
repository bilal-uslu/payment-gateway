using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Exceptions;

/// <summary>
/// Thrown when an operation is attempted on a payment that is not in a valid state for that transition.
/// </summary>
public class InvalidPaymentStateException : DomainException
{
    public InvalidPaymentStateException(string operation, PaymentStatus currentStatus)
        : base($"Cannot {operation} a payment with status '{currentStatus}'.") { }
}
