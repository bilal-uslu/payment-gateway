using FluentAssertions;

using PaymentGateway.Api.Mappers;
using DomainPaymentStatus = PaymentGateway.Domain.Enums.PaymentStatus;
using ApiPaymentStatus = PaymentGateway.Api.Enums.PaymentStatus;

namespace PaymentGateway.Api.Tests.Mappers;

public class PaymentStatusMapperTests
{
    [Theory]
    [InlineData(DomainPaymentStatus.Authorized, ApiPaymentStatus.Authorized)]
    [InlineData(DomainPaymentStatus.Declined, ApiPaymentStatus.Declined)]
    [InlineData(DomainPaymentStatus.Rejected, ApiPaymentStatus.Rejected)]
    public void ToApiStatus_MapsAllDomainStatusesToApiStatuses(DomainPaymentStatus domainStatus, ApiPaymentStatus expectedApiStatus)
    {
        var result = domainStatus.ToApiStatus();

        result.Should().Be(expectedApiStatus);
    }

    [Fact]
    public void ToApiStatus_WhenUnknownStatus_ThrowsInvalidOperationException()
    {
        var unknownStatus = (DomainPaymentStatus)999;

        var act = () => unknownStatus.ToApiStatus();

        act.Should().Throw<InvalidOperationException>();
    }
}
