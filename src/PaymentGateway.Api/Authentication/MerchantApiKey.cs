using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;

namespace PaymentGateway.Api.Authentication;

public class MerchantApiKey : IApiKey
{
    public string Key { get; }
    public string OwnerName { get; }
    public IReadOnlyCollection<Claim> Claims { get; }

    public MerchantApiKey(string key, string ownerName, Guid merchantId)
    {
        Key = key;
        OwnerName = ownerName;
        Claims = [new Claim(MerchantClaimTypes.MerchantId, merchantId.ToString())];
    }
}
