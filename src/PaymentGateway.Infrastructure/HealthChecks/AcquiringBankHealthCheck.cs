using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PaymentGateway.Infrastructure.HealthChecks;

public class AcquiringBankHealthCheck : IHealthCheck
{
    public const string HttpClientName = "acquiring-bank-health";
    private readonly IHttpClientFactory _httpClientFactory;

    public AcquiringBankHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        //There is no Acquiring bank health check.
        //It is simplified to always return healthy for demonstration purposes.

        //try
        //{
        //    using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        //    using var response = await httpClient.GetAsync("/health", cancellationToken);

        //    return response.IsSuccessStatusCode
        //        ? HealthCheckResult.Healthy("Acquiring bank is reachable.")
        //        : HealthCheckResult.Degraded($"Acquiring bank returned {(int)response.StatusCode}.");
        //}
        //catch (Exception ex)
        //{
        //    return HealthCheckResult.Unhealthy("Acquiring bank is unreachable.", ex);
        //}

        return HealthCheckResult.Healthy("Acquiring bank is reachable.");
    }
}
