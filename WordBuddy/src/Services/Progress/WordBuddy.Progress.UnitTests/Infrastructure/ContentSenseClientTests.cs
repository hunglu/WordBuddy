using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Polly.CircuitBreaker;
using Polly.Timeout;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Infrastructure;

public class ContentSenseClientTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }

    public static TheoryData<Exception> ResilienceFailures => new()
    {
        new TimeoutRejectedException("timeout"),
        new BrokenCircuitException("open"),
        new HttpRequestException("down"),
    };

    [Theory]
    [MemberData(nameof(ResilienceFailures))]
    public async Task ContentSenseClient_GetSessionSensesAsync_TransportFailure_ReturnsContentUnavailable(Exception exception)
    {
        Mock<IDistributedCache> cache = new();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        using HttpClient http = new(new ThrowingHandler(exception)) { BaseAddress = new Uri("http://content.test/") };
        ContentSenseClient client = new(http, cache.Object, new FixedTimeProvider(Now), NullLogger<ContentSenseClient>.Instance);

        Result<IReadOnlyList<ContentSenseDto>> result = await client.GetSessionSensesAsync(
            Guid.NewGuid(), [Guid.NewGuid()], Now.AddHours(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Content.Unavailable");
    }
}
