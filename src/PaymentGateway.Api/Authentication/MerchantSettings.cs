namespace PaymentGateway.Api.Authentication;

public class MerchantSettings
{
    public const string SectionName = "Merchants";

    public List<MerchantEntry> Entries { get; set; } = [];
}
