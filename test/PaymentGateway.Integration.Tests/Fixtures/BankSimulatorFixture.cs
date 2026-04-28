using System.Text;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace PaymentGateway.Integration.Tests.Fixtures;

/// <summary>
/// xUnit class fixture that starts a mountebank bank simulator container once
/// for the lifetime of a test class and tears it down afterwards.
/// </summary>
/// <remarks>
/// Loads the stub configuration from <c>imposters/bank_simulator.ejs</c> (copied to the
/// output directory at build time) and registers it via the mountebank admin API.
/// The imposter is hosted on <see cref="BankSimulatorPort"/> (8081) inside the container;
/// the actual host port is ephemeral and exposed through <see cref="SimulatorBaseUrl"/>.
/// </remarks>
public sealed class BankSimulatorFixture : IAsyncLifetime
{
    private const int BankSimulatorPort = 8081;
    private const int MountebankAdminPort = 2525;

    private IContainer _container = null!;

    /// <summary>
    /// The base URL (scheme + host + ephemeral port) that points to the running
    /// bank simulator imposter. Inject this into <c>AcquiringBank:BaseUrl</c>.
    /// </summary>
    public string SimulatorBaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("bbyars/mountebank:2.8.1")
            .WithPortBinding(MountebankAdminPort, true)
            .WithPortBinding(BankSimulatorPort, true)
            .WithCommand("--allowInjection")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MountebankAdminPort))
            .Build();

        await _container.StartAsync();

        var adminPort = _container.GetMappedPublicPort(MountebankAdminPort);
        var simulatorPort = _container.GetMappedPublicPort(BankSimulatorPort);

        SimulatorBaseUrl = $"http://localhost:{simulatorPort}";

        await RegisterImposterAsync(adminPort);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>
    /// Reads <c>imposters/bank_simulator.ejs</c>, patches the hardcoded port 8080 to
    /// <see cref="BankSimulatorPort"/>, and registers the imposter via <c>PUT /imposters</c>.
    /// </summary>
    private async Task RegisterImposterAsync(int adminPort)
    {
        var imposterFilePath = Path.Combine(AppContext.BaseDirectory, "imposters", "bank_simulator.ejs");
        var imposterJson = await File.ReadAllTextAsync(imposterFilePath);

        // The config file targets port 8080; redirect it to the port bound for this run.
        imposterJson = imposterJson.Replace("\"port\": 8080", $"\"port\": {BankSimulatorPort}");

        using var httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{adminPort}") };
        var content = new StringContent(imposterJson, Encoding.UTF8, "application/json");

        // PUT /imposters accepts the { "imposters": [...] } wrapper used in the config file.
        var response = await httpClient.PutAsync("/imposters", content);
        response.EnsureSuccessStatusCode();
    }
}
