using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Repositories;
using PaymentGateway.Infrastructure.AcquiringBank;
using PaymentGateway.Infrastructure.HealthChecks;
using PaymentGateway.Infrastructure.Repositories;

using Polly;

namespace PaymentGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
        services.AddSingleton<IIdempotencyCache, IdempotencyCache>();

        services.AddOptions<AcquiringBankOptions>()
            .Bind(configuration.GetSection(AcquiringBankOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IAcquiringBankClient, AcquiringBankClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AcquiringBankOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        })
        .AddResilienceHandler("acquiring-bank", builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            });

            builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            });
        });

        services.AddHttpClient(AcquiringBankHealthCheck.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AcquiringBankOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<AcquiringBankHealthCheck>("acquiring-bank", tags: ["ready"]);

        return services;
    }
}
