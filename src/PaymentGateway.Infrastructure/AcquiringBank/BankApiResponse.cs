using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.AcquiringBank;

internal sealed class BankApiResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; set; }
}
