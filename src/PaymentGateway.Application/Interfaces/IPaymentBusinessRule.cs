using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Interfaces;

public interface IPaymentBusinessRule
{
    /// <summary>
    /// Returns <c>true</c> when the rule is violated and the payment should be rejected.
    /// </summary>
    bool IsViolatedBy(Payment payment);

    string RejectionReason { get; }
}
