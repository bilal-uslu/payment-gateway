using Microsoft.Extensions.Options;

using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Settings;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Payments.Rules;

/// <summary>
/// Business rule that rejects payments whose card BIN appears in the configured blocked list.
/// </summary>
public class BlockedBinRule(IOptions<PaymentRulesSettings> options) : IPaymentBusinessRule
{
    private readonly IReadOnlyList<string> _blockedBins = options.Value.BlockedBins;

    public string RejectionReason => "Card BIN is blocked.";

    public bool IsViolatedBy(Payment payment)
    {
        var bin = payment.CardDetails.CardNumber.GetBin();
        return _blockedBins.Contains(bin, StringComparer.Ordinal);
    }
}
