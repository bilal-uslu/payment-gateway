using System.Security.Claims;

using FluentAssertions;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using PaymentGateway.Api.Authentication;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Payments.Commands.ProcessPayment;
using PaymentGateway.Application.Payments.Queries.GetPayment;

using ApiPaymentStatus = PaymentGateway.Api.Enums.PaymentStatus;
using DomainPaymentStatus = PaymentGateway.Domain.Enums.PaymentStatus;

namespace PaymentGateway.Api.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly NullLogger<PaymentsController> _logger = new();
    private readonly Guid _merchantId = Guid.NewGuid();

    private static readonly PostPaymentRequest ValidRequest = new()
    {
        CardNumber = "2222405343248877",
        ExpiryMonth = 4,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    private PaymentsController CreateController()
    {
        var controller = new PaymentsController(_mediatorMock.Object, _logger);
        var claims = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(MerchantClaimTypes.MerchantId, _merchantId.ToString())
        ]));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };
        return controller;
    }

    #region ProcessPaymentAsync

    [Fact]
    public async Task ProcessPaymentAsync_WhenIdempotencyKeyIsEmpty_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.ProcessPaymentAsync(ValidRequest, string.Empty, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenIdempotencyKeyIsWhitespace_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.ProcessPaymentAsync(ValidRequest, "   ", CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentAuthorized_ReturnsOkWithResponse()
    {
        var paymentId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessPaymentResult
            {
                Id = paymentId,
                Status = DomainPaymentStatus.Authorized,
                CardNumberLastFour = "8877",
                ExpiryMonth = 4,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "GBP",
                Amount = 100
            });

        var controller = CreateController();

        var result = await controller.ProcessPaymentAsync(ValidRequest, Guid.NewGuid().ToString(), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PostPaymentResponse>().Subject;
        response.Id.Should().Be(paymentId);
        response.Status.Should().Be(ApiPaymentStatus.Authorized);
        response.CardNumberLastFour.Should().Be("8877");
        response.Currency.Should().Be("GBP");
        response.Amount.Should().Be(100);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentDeclined_ReturnsOkWithResponse()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessPaymentResult
            {
                Id = Guid.NewGuid(),
                Status = DomainPaymentStatus.Declined,
                CardNumberLastFour = "8877",
                ExpiryMonth = 4,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "GBP",
                Amount = 100
            });

        var controller = CreateController();

        var result = await controller.ProcessPaymentAsync(ValidRequest, Guid.NewGuid().ToString(), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<PostPaymentResponse>()
            .Which.Status.Should().Be(ApiPaymentStatus.Declined);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentRejected_ReturnsUnprocessableEntity()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessPaymentResult
            {
                Id = Guid.NewGuid(),
                Status = DomainPaymentStatus.Rejected,
                CardNumberLastFour = "8877",
                ExpiryMonth = 4,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "GBP",
                Amount = 100
            });

        var controller = CreateController();

        var result = await controller.ProcessPaymentAsync(ValidRequest, Guid.NewGuid().ToString(), CancellationToken.None);

        var unprocessable = result.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        unprocessable.Value.Should().BeOfType<PostPaymentResponse>()
            .Which.Status.Should().Be(ApiPaymentStatus.Rejected);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SendsCommandWithCorrectMerchantIdAndIdempotencyKey()
    {
        var idempotencyKey = Guid.NewGuid().ToString();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ProcessPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessPaymentResult
            {
                Id = Guid.NewGuid(),
                Status = DomainPaymentStatus.Authorized,
                CardNumberLastFour = "8877",
                ExpiryMonth = 4,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "GBP",
                Amount = 100
            });

        var controller = CreateController();

        await controller.ProcessPaymentAsync(ValidRequest, idempotencyKey, CancellationToken.None);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ProcessPaymentCommand>(c =>
                c.MerchantId == _merchantId &&
                c.IdempotencyKey == idempotencyKey &&
                c.CardNumber == ValidRequest.CardNumber &&
                c.Currency == ValidRequest.Currency &&
                c.Amount == ValidRequest.Amount),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPaymentAsync

    [Fact]
    public async Task GetPaymentAsync_WhenPaymentExists_ReturnsOkWithResponse()
    {
        var paymentId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPaymentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetPaymentResult
            {
                Id = paymentId,
                Status = DomainPaymentStatus.Authorized,
                CardNumberLastFour = "8877",
                ExpiryMonth = 4,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 250
            });

        var controller = CreateController();

        var result = await controller.GetPaymentAsync(paymentId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<GetPaymentResponse>().Subject;
        response.Id.Should().Be(paymentId);
        response.Status.Should().Be(ApiPaymentStatus.Authorized);
        response.CardNumberLastFour.Should().Be("8877");
        response.Currency.Should().Be("USD");
        response.Amount.Should().Be(250);
    }

    [Fact]
    public async Task GetPaymentAsync_WhenPaymentNotFound_ReturnsNotFound()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPaymentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPaymentResult?)null);

        var controller = CreateController();

        var result = await controller.GetPaymentAsync(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetPaymentAsync_SendsQueryWithCorrectPaymentIdAndMerchantId()
    {
        var paymentId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPaymentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetPaymentResult?)null);

        var controller = CreateController();

        await controller.GetPaymentAsync(paymentId, CancellationToken.None);

        _mediatorMock.Verify(m => m.Send(
            It.Is<GetPaymentQuery>(q => q.Id == paymentId && q.MerchantId == _merchantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
