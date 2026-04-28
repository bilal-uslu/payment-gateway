using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Integration.Tests.Fixtures;

namespace PaymentGateway.Integration.Tests.PaymentTests;

/// <summary>
/// Base class for integration tests that rely on <see cref="BankSimulatorFixture"/>.
/// Provides shared factory creation and JSON deserialization options so that
/// derived test classes contain only test methods.
/// </summary>
public abstract class PaymentGatewayTestBase
{
    protected static readonly Guid TestMerchantId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    protected const string TestApiKey = "test-api-key";

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly BankSimulatorFixture _bankSimulator;

    protected PaymentGatewayTestBase(BankSimulatorFixture bankSimulator)
    {
        _bankSimulator = bankSimulator;
    }

    protected HttpClient CreateClient()
    {
        var client = CreateFactory().CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", TestApiKey);
        return client;
    }

    private WebApplicationFactory<PaymentsController> CreateFactory()
    {
        return new WebApplicationFactory<PaymentsController>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Merchants:Entries:0:MerchantId"] = TestMerchantId.ToString(),
                    ["Merchants:Entries:0:Name"] = "Test Merchant",
                    ["Merchants:Entries:0:ApiKey"] = TestApiKey,
                    ["AcquiringBank:BaseUrl"] = _bankSimulator.SimulatorBaseUrl
                });
            });
        });
    }
}
