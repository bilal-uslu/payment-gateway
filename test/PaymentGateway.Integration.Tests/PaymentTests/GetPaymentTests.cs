using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Integration.Tests.Fixtures;

namespace PaymentGateway.Integration.Tests.PaymentTests;

/// <summary>
/// Integration tests that exercise payment processing end-to-end using a real
/// mountebank bank simulator container managed by <see cref="BankSimulatorFixture"/>.
/// The container is started once for the whole class and shared across all tests.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class GetPaymentTests(BankSimulatorFixture bankSimulator)
    : PaymentGatewayTestBase(bankSimulator)
{
    [Fact]
    public async Task GetPayment_ReturnsPayment_WhenCardPaymentIsAuthorized()
    {
        // Arrange – card ending in 7 → odd → authorized
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",   // ends in 7
            ExpiryMonth = 4,
            ExpiryYear = DateTime.UtcNow.Year + 2,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/Payments", request);
        var createPaymentResponse = await createResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.NotNull(createPaymentResponse);
        Assert.Equal(PaymentStatus.Authorized, createPaymentResponse.Status);
        Assert.Equal("8877", createPaymentResponse.CardNumberLastFour);
        Assert.Equal("GBP", createPaymentResponse.Currency);
        Assert.Equal(100, createPaymentResponse.Amount);


        // Act
        var response = await client.GetAsync($"/api/v1/Payments/{createPaymentResponse.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Authorized, paymentResponse.Status);
        Assert.Equal("8877", paymentResponse.CardNumberLastFour);
        Assert.Equal("GBP", paymentResponse.Currency);
        Assert.Equal(100, paymentResponse.Amount);
    }

    [Fact]
    public async Task GetPayment_ReturnsPayment_WhenCardPaymentIsDeclined()
    {
        // Arrange – card ending in 2 → even → declined
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248112",   // ends in 2
            ExpiryMonth = 4,
            ExpiryYear = DateTime.UtcNow.Year + 2,
            Currency = "USD",
            Amount = 500,
            Cvv = "456"
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/Payments", request);
        var createPaymentResponse = await createResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.NotNull(createPaymentResponse);
        Assert.Equal(PaymentStatus.Declined, createPaymentResponse.Status);

        // Act
        var response = await client.GetAsync($"/api/v1/Payments/{createPaymentResponse.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Declined, paymentResponse.Status);
    }

    [Fact]
    public async Task ProcessPayment_ReturnsRejected_WhenCardIsNotAccepted()
    {
        // Arrange – card ending in 2 → even → declined
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var request = new PostPaymentRequest
        {
            CardNumber = "1234565343248112",   // ends in 2
            ExpiryMonth = 4,
            ExpiryYear = DateTime.UtcNow.Year + 2,
            Currency = "USD",
            Amount = 500,
            Cvv = "456"
        };


        var createResponse = await client.PostAsJsonAsync("/api/v1/Payments", request);
        var createPaymentResponse = await createResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, createResponse.StatusCode);
        Assert.NotNull(createPaymentResponse);
        Assert.Equal(PaymentStatus.Rejected, createPaymentResponse.Status);


        // Act
        var response = await client.GetAsync($"/api/v1/Payments/{createPaymentResponse.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Rejected, paymentResponse.Status);
    }
}
