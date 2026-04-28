namespace PaymentGateway.Application.Settings;

public class PaymentRulesSettings
{
    public const string SectionName = "PaymentRules";

    public IReadOnlyList<string> BlockedBins { get; init; } = [];
}
