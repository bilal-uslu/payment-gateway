namespace PaymentGateway.Application.Models;

public class AcquiringBankRequest
{
    public required string CardNumber { get; set; }
    public required string ExpiryDate { get; set; }
    public required string Currency { get; set; } 
    public long Amount { get; set; }
    public required string Cvv { get; set; }
}
