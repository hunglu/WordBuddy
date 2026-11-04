using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Infrastructure.ContentClient;

namespace WordBuddy.Progress.UnitTests.Infrastructure;

public class SessionSenseCacheTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IDistributedCache> _cache = new();

    private SessionSenseCache Create() => new(_cache.Object, new FixedTimeProvider(Now));

    private static List<ContentSenseDto> OneSense() => [new(Guid.NewGuid(), "cat", "an animal", null, null, null)];

    [Fact]
    public void SessionSenseCache_CacheKey_UsesSessionSensesFormat()
    {
        Guid sessionId = Guid.NewGuid();

        SessionSenseCache.CacheKey(sessionId).Should().Be($"progress:session-senses:{sessionId}");
    }

    [Fact]
    public async Task SessionSenseCache_SetAsync_FutureExpiry_StoresUnderKeyWithAbsoluteExpiry()
    {
        Guid sessionId = Guid.NewGuid();
        DateTime expires = Now.AddHours(2);

        await Create().SetAsync(sessionId, OneSense(), expires, CancellationToken.None);

        _cache.Verify(c => c.SetAsync(
            SessionSenseCache.CacheKey(sessionId),
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpiration == new DateTimeOffset(expires)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SessionSenseCache_SetAsync_ExpiryInThePast_IsNotCached()
    {
        await Create().SetAsync(Guid.NewGuid(), OneSense(), Now.AddMinutes(-1), CancellationToken.None);

        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SessionSenseCache_GetAsync_Miss_ReturnsNull()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        (await Create().GetAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeNull();
    }
}
