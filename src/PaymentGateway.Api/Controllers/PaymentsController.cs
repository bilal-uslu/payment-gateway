using System.Security.Claims;

using Asp.Versioning;
using MediatR;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using PaymentGateway.Api.Authentication;
using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Mappers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Application.Payments.Queries.GetPayment;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Controllers;

/// <summary>
/// Provides endpoints for processing and retrieving payment transactions.
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
[ApiVersion(1.0)]
[EnableRateLimiting(RateLimitingExtensions.PerMerchantPolicy)]
[Produces("application/json")]
[Consumes("application/json")]
[Tags("Payments")]
public class PaymentsController(IMediator mediator, ILogger<PaymentsController> logger) : Controller
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<PaymentsController> _logger = logger;

    /// <summary>
    /// Processes a new payment through the payment gateway.
    /// </summary>
    /// <param name="request">The payment request containing card details and transaction information.</param>
    /// <param name="idempotencyKey">The idempotency key to ensure the request is processed only once.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The payment processing result including payment ID and authorization status.</returns>
    /// <response code="200">Payment was successfully processed (either authorized or declined by the bank).</response>
    /// <response code="400">Invalid payment request - validation failed.</response>
    /// <response code="422">Payment was rejected due to invalid information without calling the acquiring bank.</response>
    /// <response code="500">An unexpected error occurred while processing the payment.</response>
    /// <response code="503">The acquiring bank service is unavailable.</response>
    [HttpPost]
    [EndpointName("ProcessPayment")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync(
        [FromBody] PostPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _logger.LogWarning("ProcessPayment request received with missing Idempotency-Key header");
            return BadRequest(new ProblemDetails { Title = "Missing Idempotency-Key", Detail = "The 'Idempotency-Key' header is required." });
        }

        var merchantId = Guid.Parse(User.FindFirstValue(MerchantClaimTypes.MerchantId)!);

        _logger.LogInformation(
            "Processing payment for MerchantId {MerchantId} with IdempotencyKey {IdempotencyKey}",
            merchantId, idempotencyKey);

        var command = request.ToCommand(merchantId, idempotencyKey);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} processed with status {PaymentStatus} for MerchantId {MerchantId}",
            result.Id, result.Status, merchantId);

        if (result.Status == PaymentStatus.Rejected)
        {
            return UnprocessableEntity(result.ToResponse());
        }

        return Ok(result.ToResponse());
    }

    /// <summary>
    /// Retrieves the details of a previously processed payment.
    /// </summary>
    /// <param name="id">The unique identifier of the payment to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The payment details including masked card information and transaction status.</returns>
    /// <response code="200">Payment details were successfully retrieved.</response>
    /// <response code="404">Payment with the specified ID was not found.</response>
    /// <response code="500">An unexpected error occurred while retrieving the payment.</response>
    [HttpGet("{id:guid}")]
    [EndpointName("GetPayment")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetPaymentResponse>> GetPaymentAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var merchantId = Guid.Parse(User.FindFirstValue(MerchantClaimTypes.MerchantId)!);

        _logger.LogInformation(
            "Retrieving payment {PaymentId} for MerchantId {MerchantId}",
            id, merchantId);

        var query = new GetPaymentQuery { Id = id, MerchantId = merchantId };

        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
        {
            _logger.LogWarning(
                "Payment {PaymentId} not found for MerchantId {MerchantId}",
                id, merchantId);
            return NotFound();
        }

        return Ok(result.ToResponse());
    }
}