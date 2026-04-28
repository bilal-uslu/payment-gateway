using System.Text.Json;

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Middleware;

namespace PaymentGateway.Api.Tests.Middleware;

public class ValidationExceptionHandlerTests
{
    private readonly ValidationExceptionHandler _handler = new();

    [Fact]
    public async Task TryHandleAsync_WhenExceptionIsNotValidationException_ReturnsFalse()
    {
        var context = new DefaultHttpContext();

        var handled = await _handler.TryHandleAsync(context, new InvalidOperationException("some error"), CancellationToken.None);

        handled.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_WhenValidationException_ReturnsTrueAndSetsBadRequestStatus()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var failures = new List<ValidationFailure>
        {
            new("CardNumber", "Card number is required.")
        };
        var exception = new ValidationException(failures);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task TryHandleAsync_WhenValidationException_WritesValidationProblemDetailsBody()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var failures = new List<ValidationFailure>
        {
            new("CardNumber", "Card number is invalid."),
            new("Currency", "Currency is required.")
        };
        var exception = new ValidationException(failures);

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Validation failed");
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
        problemDetails.Errors.Should().ContainKey("CardNumber");
        problemDetails.Errors.Should().ContainKey("Currency");
    }

    [Fact]
    public async Task TryHandleAsync_WhenMultipleErrorsForSameProperty_GroupsThemTogether()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var failures = new List<ValidationFailure>
        {
            new("Amount", "Amount must be positive."),
            new("Amount", "Amount must not exceed 1000000.")
        };
        var exception = new ValidationException(failures);

        await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        problemDetails!.Errors["Amount"].Should().HaveCount(2);
    }
}
