using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Application.Payments.Queries.GetPayment;

public class GetPaymentResult
{
    public Guid Id { get; set; }
    public PaymentStatus Status { get; set; }
    public required string CardNumberLastFour { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public required string Currency { get; set; }
    public long Amount { get; set; }
}
