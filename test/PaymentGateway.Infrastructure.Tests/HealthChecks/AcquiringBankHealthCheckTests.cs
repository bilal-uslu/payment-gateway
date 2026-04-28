using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using PaymentGateway.Infrastructure.HealthChecks;

namespace PaymentGateway.Infrastructure.Tests.HealthChecks;

public class AcquiringBankHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_AlwaysReturnsHealthy()
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var healthCheck = new AcquiringBankHealthCheck(httpClientFactoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("acquiring-bank", healthCheck, HealthStatus.Unhealthy, null)
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Acquiring bank is reachable.");
    }

    [Fact]
    public async Task CheckHealthAsync_DoesNotCallHttpClientFactory()
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var healthCheck = new AcquiringBankHealthCheck(httpClientFactoryMock.Object);
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("acquiring-bank", healthCheck, HealthStatus.Unhealthy, null)
        };

        await healthCheck.CheckHealthAsync(context);

        httpClientFactoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }
}
