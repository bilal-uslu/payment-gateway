namespace PaymentGateway.Api.Authentication;

public class MerchantEntry
{
    public Guid MerchantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
