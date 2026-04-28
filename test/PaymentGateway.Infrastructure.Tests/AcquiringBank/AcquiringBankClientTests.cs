using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using PaymentGateway.Application.Models;
using PaymentGateway.Infrastructure.AcquiringBank;

namespace PaymentGateway.Infrastructure.Tests.AcquiringBank;

public class AcquiringBankClientTests
{
    private static AcquiringBankClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://bank-simulator") };
        return new AcquiringBankClient(httpClient, new NullLogger<AcquiringBankClient>());
    }

    private static Mock<HttpMessageHandler> CreateHandlerMock(HttpStatusCode statusCode, object? responseBody)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var json = JsonSerializer.Serialize(responseBody);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        return handlerMock;
    }

    private static AcquiringBankRequest ValidRequest => new()
    {
        CardNumber = "2222405343248877",
        ExpiryDate = "04/2026",
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankAuthorizes_ReturnsAuthorizedResponse()
    {
        var bankResponse = new { authorized = true, authorization_code = "AUTH-XYZ" };
        var client = CreateClient(CreateHandlerMock(HttpStatusCode.OK, bankResponse).Object);

        var result = await client.ProcessPaymentAsync(ValidRequest);

        result.Authorized.Should().BeTrue();
        result.AuthorizationCode.Should().Be("AUTH-XYZ");
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankDeclines_ReturnsUnauthorizedResponse()
    {
        var bankResponse = new { authorized = false, authorization_code = (string?)null };
        var client = CreateClient(CreateHandlerMock(HttpStatusCode.OK, bankResponse).Object);

        var result = await client.ProcessPaymentAsync(ValidRequest);

        result.Authorized.Should().BeFalse();
        result.AuthorizationCode.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankReturnsNonSuccess_ThrowsHttpRequestException()
    {
        var client = CreateClient(CreateHandlerMock(HttpStatusCode.InternalServerError, null).Object);

        var act = async () => await client.ProcessPaymentAsync(ValidRequest);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ProcessPaymentAsync_SendsCorrectPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        var bankResponse = new { authorized = true, authorization_code = "AUTH-001" };
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(bankResponse), System.Text.Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);
        await client.ProcessPaymentAsync(ValidRequest);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/payments");
        capturedRequest.Method.Should().Be(HttpMethod.Post);

        var body = await capturedRequest.Content!.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("card_number").GetString().Should().Be(ValidRequest.CardNumber);
        body.GetProperty("currency").GetString().Should().Be(ValidRequest.Currency);
        body.GetProperty("amount").GetInt64().Should().Be(ValidRequest.Amount);
        body.GetProperty("cvv").GetString().Should().Be(ValidRequest.Cvv);
    }
}
