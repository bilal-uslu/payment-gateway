using MediatR;

using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommand : IRequest<ProcessPaymentResult>, IIdempotentRequest<ProcessPaymentResult>
{
    public Guid MerchantId { get; set; }
    public required string CardNumber { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public required string Currency { get; set; }
    public long Amount { get; set; }
    public required string Cvv { get; set; }
    public required string IdempotencyKey { get; set; }
}
