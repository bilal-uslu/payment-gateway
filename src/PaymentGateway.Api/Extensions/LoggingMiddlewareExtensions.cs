using Serilog;

using PaymentGateway.Api.Middleware;

namespace PaymentGateway.Api.Extensions;

internal static class LoggingMiddlewareExtensions
{
    internal static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    internal static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms [{CorrelationId}]";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
                {
                    diagnosticContext.Set("CorrelationId", correlationId);
                }
            };
        });
    }
}
