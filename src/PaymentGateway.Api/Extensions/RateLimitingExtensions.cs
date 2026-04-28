using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace PaymentGateway.Api.Extensions;

internal static class RateLimitingExtensions
{
    internal const string PerMerchantPolicy = "per_merchant";

    internal static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var perMerchantLimit = configuration.GetValue("RateLimiting:PerMerchant:PermitLimit", 100);
        var perMerchantWindow = configuration.GetValue("RateLimiting:PerMerchant:WindowSeconds", 60);

        var perIpLimit = configuration.GetValue("RateLimiting:PerIp:PermitLimit", 20);
        var perIpWindow = configuration.GetValue("RateLimiting:PerIp:WindowSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Per-merchant policy keyed by the API key header value; falls back to IP address.
            options.AddPolicy(PerMerchantPolicy, context =>
            {
                var apiKey = context.Request.Headers["X-API-Key"].ToString();

                if (!string.IsNullOrEmpty(apiKey))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"merchant:{apiKey}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = perMerchantLimit,
                            Window = TimeSpan.FromSeconds(perMerchantWindow),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                }

                // Unauthenticated – stricter limit by IP.
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"ip:{ip}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = perIpLimit,
                        Window = TimeSpan.FromSeconds(perIpWindow),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
