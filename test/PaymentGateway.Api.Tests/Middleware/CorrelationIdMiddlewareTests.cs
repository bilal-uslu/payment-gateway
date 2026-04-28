using FluentAssertions;
using Microsoft.AspNetCore.Http;

using PaymentGateway.Api.Middleware;

namespace PaymentGateway.Api.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdHeaderPresent_UsesThatValue()
    {
        var correlationId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        string? capturedCorrelationId = null;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            capturedCorrelationId = ctx.Items[CorrelationIdMiddleware.HeaderName] as string;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        capturedCorrelationId.Should().Be(correlationId);
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdHeaderMissing_GeneratesNewCorrelationId()
    {
        var context = new DefaultHttpContext();

        string? capturedCorrelationId = null;
        var middleware = new CorrelationIdMiddleware(ctx =>
        {
            capturedCorrelationId = ctx.Items[CorrelationIdMiddleware.HeaderName] as string;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        capturedCorrelationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(capturedCorrelationId, out _).Should().BeTrue();
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(capturedCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_PropagatesCorrelationIdInResponseHeader()
    {
        var correlationId = "my-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be(correlationId);
    }
}
