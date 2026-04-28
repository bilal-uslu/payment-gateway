using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.Options;

namespace PaymentGateway.Api.Authentication;

public class MerchantApiKeyProvider : IApiKeyProvider
{
    private readonly MerchantSettings _settings;

    public MerchantApiKeyProvider(IOptions<MerchantSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<IApiKey?> ProvideAsync(string key)
    {
        var merchant = _settings.Entries.FirstOrDefault(m => m.ApiKey == key);

        if (merchant is null)
        {
            return Task.FromResult<IApiKey?>(null);
        }

        return Task.FromResult<IApiKey?>(new MerchantApiKey(key, merchant.Name, merchant.MerchantId));
    }
}
