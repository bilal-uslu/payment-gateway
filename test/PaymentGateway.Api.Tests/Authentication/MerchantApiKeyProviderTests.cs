using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

using PaymentGateway.Api.Authentication;

namespace PaymentGateway.Api.Tests.Authentication;

public class MerchantApiKeyProviderTests
{
    private static MerchantApiKeyProvider CreateProvider(IEnumerable<MerchantEntry> entries)
    {
        var settings = new MerchantSettings { Entries = [.. entries] };
        var options = new Mock<IOptions<MerchantSettings>>();
        options.Setup(o => o.Value).Returns(settings);
        return new MerchantApiKeyProvider(options.Object);
    }

    [Fact]
    public async Task ProvideAsync_WhenApiKeyExists_ReturnsMerchantApiKey()
    {
        var merchantId = Guid.NewGuid();
        var apiKey = "test-api-key-123";
        var provider = CreateProvider([
            new MerchantEntry { MerchantId = merchantId, Name = "Test Merchant", ApiKey = apiKey }
        ]);

        var result = await provider.ProvideAsync(apiKey);

        result.Should().NotBeNull();
        result!.Key.Should().Be(apiKey);
        result.OwnerName.Should().Be("Test Merchant");
    }

    [Fact]
    public async Task ProvideAsync_WhenApiKeyDoesNotExist_ReturnsNull()
    {
        var provider = CreateProvider([
            new MerchantEntry { MerchantId = Guid.NewGuid(), Name = "Test Merchant", ApiKey = "valid-key" }
        ]);

        var result = await provider.ProvideAsync("unknown-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ProvideAsync_WhenNoMerchantsConfigured_ReturnsNull()
    {
        var provider = CreateProvider([]);

        var result = await provider.ProvideAsync("any-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ProvideAsync_WhenApiKeyExists_IncludesMerchantIdClaim()
    {
        var merchantId = Guid.NewGuid();
        var apiKey = "test-api-key-456";
        var provider = CreateProvider([
            new MerchantEntry { MerchantId = merchantId, Name = "Merchant A", ApiKey = apiKey }
        ]);

        var result = await provider.ProvideAsync(apiKey);

        result!.Claims.Should().Contain(c =>
            c.Type == MerchantClaimTypes.MerchantId &&
            c.Value == merchantId.ToString());
    }

    [Fact]
    public async Task ProvideAsync_WhenMultipleMerchantsConfigured_ReturnsCorrectMerchant()
    {
        var merchantIdA = Guid.NewGuid();
        var merchantIdB = Guid.NewGuid();
        var provider = CreateProvider([
            new MerchantEntry { MerchantId = merchantIdA, Name = "Merchant A", ApiKey = "key-a" },
            new MerchantEntry { MerchantId = merchantIdB, Name = "Merchant B", ApiKey = "key-b" }
        ]);

        var result = await provider.ProvideAsync("key-b");

        result.Should().NotBeNull();
        result!.OwnerName.Should().Be("Merchant B");
        result.Claims.Should().Contain(c =>
            c.Type == MerchantClaimTypes.MerchantId &&
            c.Value == merchantIdB.ToString());
    }
}
