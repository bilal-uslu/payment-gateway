using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using PaymentGateway.Infrastructure.Repositories;

namespace PaymentGateway.Infrastructure.Tests.Repositories;

public class IdempotencyCacheTests
{
    private static IdempotencyCache CreateCache()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        return new IdempotencyCache(memoryCache);
    }

    [Fact]
    public async Task TryGetAsync_WhenKeyNotSet_ReturnsFalseAndDefault()
    {
        var cache = CreateCache();

        var (found, response) = await cache.TryGetAsync<string>("missing-key");

        found.Should().BeFalse();
        response.Should().BeNull();
    }

    [Fact]
    public async Task TryGetAsync_WhenKeySet_ReturnsTrueWithCachedValue()
    {
        var cache = CreateCache();
        await cache.SetAsync("key1", "my-response");

        var (found, response) = await cache.TryGetAsync<string>("key1");

        found.Should().BeTrue();
        response.Should().Be("my-response");
    }

    [Fact]
    public async Task SetAsync_StoresValue_RetrievableByTheSameKey()
    {
        var cache = CreateCache();
        await cache.SetAsync<string>("key2", "stored-value");

        var (found, response) = await cache.TryGetAsync<string>("key2");

        found.Should().BeTrue();
        response.Should().Be("stored-value");
    }

    [Fact]
    public async Task TryGetAsync_DifferentResponseTypes_DoNotCollide()
    {
        var cache = CreateCache();
        await cache.SetAsync<string>("shared-key", "string-value");
        await cache.SetAsync<int>("shared-key", 42);

        var (foundString, stringVal) = await cache.TryGetAsync<string>("shared-key");
        var (foundInt, intVal) = await cache.TryGetAsync<int>("shared-key");

        foundString.Should().BeTrue();
        stringVal.Should().Be("string-value");
        foundInt.Should().BeTrue();
        intVal.Should().Be(42);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValueForSameKey()
    {
        var cache = CreateCache();
        await cache.SetAsync<string>("key3", "first");
        await cache.SetAsync<string>("key3", "second");

        var (found, response) = await cache.TryGetAsync<string>("key3");

        found.Should().BeTrue();
        response.Should().Be("second");
    }
}
