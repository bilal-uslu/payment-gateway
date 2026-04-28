namespace PaymentGateway.Application.Models;

public class AcquiringBankResponse
{
    public bool Authorized { get; set; }
    public string? AuthorizationCode { get; set; }
}
