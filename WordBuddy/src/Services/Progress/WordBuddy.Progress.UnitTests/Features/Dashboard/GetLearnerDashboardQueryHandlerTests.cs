using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Dashboard;
using WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.Dashboard;

public class GetLearnerDashboardQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 20, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IDashboardReadRepository> _repository = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Guid _learnerId = Guid.NewGuid();

    public GetLearnerDashboardQueryHandlerTests()
    {
        _repository
            .Setup(r => r.GetReviewLogsAsync(_learnerId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<DashboardReviewRow>>([]));
        _repository
            .Setup(r => r.GetWordStatesAsync(_learnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<DashboardWordRow>>([]));
        _repository
            .Setup(r => r.GetMembershipsAsync(_learnerId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<DashboardMembershipRow>>([]));
        _repository
            .Setup(r => r.GetSessionIssuesAsync(_learnerId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<DashboardSessionRow>>([]));
    }

    private GetLearnerDashboardQueryHandler CreateHandler() =>
        new(
            _repository.Object,
            new DashboardCalculator(new DashboardOptions()),
            _cache.Object,
            new FixedTimeProvider(Now),
            new GetLearnerDashboardQueryValidator(),
            Mock.Of<ILogger<GetLearnerDashboardQueryHandler>>());

    private void GivenCached(string key, LearnerDashboardDto dto) =>
        _cache
            .Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto)));

    [Fact]
    public async Task GetLearnerDashboardQueryHandler_HandleAsync_CacheMissComputesAndStoresWithAbsoluteExpiry()
    {
        string key = GetLearnerDashboardQueryHandler.CacheKey(_learnerId, 30, TimeSpan.FromHours(7));
        DistributedCacheEntryOptions? stored = null;
        _cache
            .Setup(c => c.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, _, options, _) => stored = options)
            .Returns(Task.CompletedTask);

        Result<LearnerDashboardDto> result = await CreateHandler()
            .HandleAsync(new GetLearnerDashboardQuery(_learnerId, "2026-10-09T03:00:00+07:00", 30));

        result.IsSuccess.Should().BeTrue();
        result.Value.LearnerId.Should().Be(_learnerId);
        result.Value.To.Should().Be(new DateOnly(2026, 10, 9));
        stored.Should().NotBeNull();
        stored!.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
        stored.SlidingExpiration.Should().BeNull();
        _repository.Verify(r => r.GetReviewLogsAsync(_learnerId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLearnerDashboardQueryHandler_HandleAsync_CacheHitSkipsRepository()
    {
        string key = GetLearnerDashboardQueryHandler.CacheKey(_learnerId, 30, TimeSpan.Zero);
        LearnerDashboardDto cached = new DashboardCalculator(new DashboardOptions())
            .Calculate(_learnerId, 30, Now, TimeSpan.Zero, [], [], [], []);
        GivenCached(key, cached);

        Result<LearnerDashboardDto> result = await CreateHandler().HandleAsync(new GetLearnerDashboardQuery(_learnerId, null, 30));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cached);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetLearnerDashboardQueryHandler_CacheKey_DiffersByDaysAndOffset()
    {
        GetLearnerDashboardQueryHandler.CacheKey(_learnerId, 7, TimeSpan.Zero)
            .Should().NotBe(GetLearnerDashboardQueryHandler.CacheKey(_learnerId, 30, TimeSpan.Zero));
        GetLearnerDashboardQueryHandler.CacheKey(_learnerId, 7, TimeSpan.Zero)
            .Should().NotBe(GetLearnerDashboardQueryHandler.CacheKey(_learnerId, 7, TimeSpan.FromHours(7)));
        GetLearnerDashboardQueryHandler.CacheKey(_learnerId, 7, TimeSpan.FromHours(7))
            .Should().Be($"progress:dashboard:{_learnerId}:7:420");
    }

    [Fact]
    public async Task GetLearnerDashboardQueryHandler_HandleAsync_RepositoryFailureReturnsFailureAndSkipsCache()
    {
        Error error = Error.Failure("Db.Down", "down");
        _repository
            .Setup(r => r.GetReviewLogsAsync(_learnerId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<DashboardReviewRow>>(error));

        Result<LearnerDashboardDto> result = await CreateHandler().HandleAsync(new GetLearnerDashboardQuery(_learnerId, null, 30));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLearnerDashboardQueryHandler_HandleAsync_InvalidDaysReturnsValidationFailure()
    {
        Result<LearnerDashboardDto> result = await CreateHandler().HandleAsync(new GetLearnerDashboardQuery(_learnerId, null, 14));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLearnerDashboardQueryHandler_HandleAsync_BadClientHeaderReturnsValidationFailure()
    {
        Result<LearnerDashboardDto> result = await CreateHandler().HandleAsync(new GetLearnerDashboardQuery(_learnerId, "not-a-date", 30));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ClientDateTime.Invalid");
    }
}
