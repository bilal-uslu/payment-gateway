using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Models;

namespace PaymentGateway.Infrastructure.AcquiringBank;

public class AcquiringBankClient(HttpClient httpClient, ILogger<AcquiringBankClient> logger) : IAcquiringBankClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AcquiringBankClient> _logger = logger;

    public async Task<AcquiringBankResponse> ProcessPaymentAsync(AcquiringBankRequest request, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            card_number = request.CardNumber,
            expiry_date = request.ExpiryDate,
            currency = request.Currency,
            amount = request.Amount,
            cvv = request.Cvv
        };

        //Acquiring banmk has no authentication, it is directly called for demonstration purposes.
        //In real life, you would have to implement some kind of authentication mechanism, such as API keys or OAuth.

        _logger.LogInformation(
            "Sending payment request to acquiring bank: CardNumberLastFour={CardNumberLastFour}, ExpiryDate={ExpiryDate}, Currency={Currency}, Amount={Amount}",
            request.CardNumber[^4..], request.ExpiryDate, request.Currency, request.Amount);

        var response = await _httpClient.PostAsJsonAsync("/payments", body, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Acquiring bank returned an unsuccessful status code {StatusCode}",
                (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var bankResponse = await response.Content.ReadFromJsonAsync<BankApiResponse>(cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Acquiring bank response received: Authorized={Authorized}",
            bankResponse!.Authorized);

        return new AcquiringBankResponse
        {
            Authorized = bankResponse.Authorized,
            AuthorizationCode = bankResponse.AuthorizationCode
        };
    }
    }
