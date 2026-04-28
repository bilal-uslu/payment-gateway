using MediatR;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Models;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Repositories;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Payments.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler(
    IAcquiringBankClient acquiringBankClient,
    IPaymentsRepository paymentsRepository,
    IEnumerable<IPaymentBusinessRule> businessRules,
    ILogger<ProcessPaymentCommandHandler> logger) : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    public async Task<ProcessPaymentResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var cardDetails = CardDetails.Create(
            CardNumber.Create(request.CardNumber),
            ExpiryDate.Create(request.ExpiryMonth, request.ExpiryYear),
            CardVerificationValue.Create(request.Cvv));

        var money = Money.Create(request.Amount, request.Currency);

        var payment = Payment.Create(request.MerchantId, cardDetails, money);

        var violatedRule = businessRules.FirstOrDefault(rule => rule.IsViolatedBy(payment));
        if (violatedRule is not null)
        {
            logger.LogWarning(
                "Payment {PaymentId} rejected for MerchantId {MerchantId} due to violated rule {RuleName}",
                payment.Id, request.MerchantId, violatedRule.GetType().Name);

            payment.Reject();
            await paymentsRepository.AddAsync(payment, cancellationToken);

            return new ProcessPaymentResult
            {
                Id = payment.Id,
                Status = payment.Status,
                CardNumberLastFour = cardDetails.GetLastFourDigits(),
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount
            };
        }

        var bankRequest = new AcquiringBankRequest
        {
            CardNumber = request.CardNumber,
            ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv
        };

        logger.LogInformation(
            "Sending payment {PaymentId} to acquiring bank for MerchantId {MerchantId}",
            payment.Id, request.MerchantId);

        var bankResponse = await acquiringBankClient.ProcessPaymentAsync(bankRequest, cancellationToken);

        if (bankResponse.Authorized && !string.IsNullOrWhiteSpace(bankResponse.AuthorizationCode))
        {
            logger.LogInformation(
                "Payment {PaymentId} authorized by acquiring bank with AuthorizationCode {AuthorizationCode}",
                payment.Id, bankResponse.AuthorizationCode);
            payment.Authorize(bankResponse.AuthorizationCode);
        }
        else
        {
            logger.LogInformation("Payment {PaymentId} declined by acquiring bank", payment.Id);
            payment.Decline();
        }

        await paymentsRepository.AddAsync(payment, cancellationToken);

        return new ProcessPaymentResult
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = cardDetails.GetLastFourDigits(),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount
        };
    }
}
