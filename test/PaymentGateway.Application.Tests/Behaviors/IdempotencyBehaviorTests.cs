using FluentAssertions;
using MediatR;
using Moq;
using PaymentGateway.Application.Behaviors;
using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Application.Tests.Behaviors;

public class IdempotencyBehaviorTests
{
    private readonly Mock<IIdempotencyCache> _cacheMock = new();

    private IdempotencyBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
        where TRequest : notnull
        => new(_cacheMock.Object);

    [Fact]
    public async Task Handle_WhenRequestIsNotIdempotent_CallsNext()
    {
        var behavior = CreateBehavior<NonIdempotentRequest, string>();
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ => { nextCalled = true; return Task.FromResult("result"); };

        var result = await behavior.Handle(new NonIdempotentRequest(), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be("result");
        _cacheMock.Verify(c => c.TryGetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedResponseWithoutCallingNext()
    {
        var request = new IdempotentRequest { MerchantId = Guid.NewGuid(), IdempotencyKey = "key1" };
        var cachedResponse = "cached";

        _cacheMock
            .Setup(c => c.TryGetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, cachedResponse));

        var behavior = CreateBehavior<IdempotentRequest, string>();
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ => { nextCalled = true; return Task.FromResult("fresh"); };

        var result = await behavior.Handle(request, next, CancellationToken.None);

        nextCalled.Should().BeFalse();
        result.Should().Be("cached");
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_CallsNextAndCachesResponse()
    {
        var request = new IdempotentRequest { MerchantId = Guid.NewGuid(), IdempotencyKey = "key2" };

        _cacheMock
            .Setup(c => c.TryGetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null));

        var behavior = CreateBehavior<IdempotentRequest, string>();
        RequestHandlerDelegate<string> next = _ => Task.FromResult("fresh");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().Be("fresh");
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), "fresh", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CacheScopedKeyIncludesMerchantIdAndIdempotencyKey()
    {
        var merchantId = Guid.NewGuid();
        var idempotencyKey = "unique-key";
        var request = new IdempotentRequest { MerchantId = merchantId, IdempotencyKey = idempotencyKey };
        var expectedKey = $"{merchantId}:{idempotencyKey}";

        _cacheMock
            .Setup(c => c.TryGetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, null));

        var behavior = CreateBehavior<IdempotentRequest, string>();
        await behavior.Handle(request, _ => Task.FromResult("x"), CancellationToken.None);

        _cacheMock.Verify(c => c.TryGetAsync<string>(expectedKey, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _cacheMock.Verify(c => c.SetAsync(expectedKey, "x", It.IsAny<CancellationToken>()), Times.Once);
    }

    private class NonIdempotentRequest { }

    private class IdempotentRequest : IIdempotentRequest<string>
    {
        public Guid MerchantId { get; set; }
        public required string IdempotencyKey { get; set; }
    }
}
