using MediatR;

namespace PaymentGateway.Application.Payments.Queries.GetPayment;

public class GetPaymentQuery : IRequest<GetPaymentResult?>
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
}
