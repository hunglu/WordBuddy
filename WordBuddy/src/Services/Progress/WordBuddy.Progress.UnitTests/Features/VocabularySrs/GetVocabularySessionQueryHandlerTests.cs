using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetVocabularySession;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.VocabularySrs;

/// <summary>Child and adult use the same session rule (D-3), so the handler takes no age group;
/// the cap differs only by backlog and by the user's own setting.</summary>
public class GetVocabularySessionQueryHandlerTests
{
    // 2026-10-07 20:00 UTC = 2026-10-08 03:00 at +07:00.
    private static readonly DateTime Now = new(2026, 10, 7, 20, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILearnerWordStateRepository> _states = new();
    private readonly Mock<IVocabularyLearnerSettingsRepository> _settings = new();
    private readonly Mock<IVocabularySessionIssueRepository> _issues = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly VocabularySchedulingOptions _options = new() { MaxDueItems = 50 };

    public GetVocabularySessionQueryHandlerTests()
    {
        _settings
            .Setup(s => s.GetAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<VocabularyLearnerSettings>(Error.NotFound("VocabularySettings.NotFound", "none")));
        _issues
            .Setup(i => i.AddAsync(It.IsAny<VocabularySessionIssue>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _issues
            .Setup(i => i.GetOpenAsync(_userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<VocabularySessionIssue>(Error.NotFound("VocabularySession.NoOpenSession", "none")));
        _issues
            .Setup(i => i.GetAnsweredSenseIdsAsync(_userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlySet<Guid>>(new HashSet<Guid>()));
        _states.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        GivenDue([], dueCount: 0);
        GivenIntroducedToday(0);
        _states
            .Setup(s => s.GetNewInAddedOrderAsync(_userId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, int take, CancellationToken _) =>
                Result.Success<IReadOnlyList<LearnerWordState>>(
                    Enumerable.Range(0, take).Select(_ => LearnerWordState.CreateNew(Guid.NewGuid(), _userId, Guid.NewGuid(), Now)).ToList()));
    }

    private GetVocabularySessionQueryHandler CreateHandler() =>
        new(_states.Object, _settings.Object, _issues.Object, new NewWordCapPolicy(new NewWordCapOptions()), _options,
            new FixedTimeProvider(Now), Mock.Of<ILogger<GetVocabularySessionQueryHandler>>());

    private void GivenDue(IReadOnlyList<LearnerWordState> due, int dueCount)
    {
        _states.Setup(s => s.CountDueAsync(_userId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(dueCount));
        _states.Setup(s => s.GetDueAsync(_userId, Now, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(due));
    }

    private void GivenIntroducedToday(int count) =>
        _states
            .Setup(s => s.CountFirstReviewedBetweenAsync(_userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(count));

    private static LearnerWordState ReviewedState(Guid userId, DateTime reviewedAt)
    {
        VocabularySchedulingOptions options = new();
        LearnerWordState state = LearnerWordState.CreateNew(Guid.NewGuid(), userId, Guid.NewGuid(), reviewedAt);
        state.ApplyReview(FsrsRating.Good, reviewedAt, new FsrsScheduler(options), options);
        return state;
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_WritesOneSessionIssueWithPlannedCount()
    {
        LearnerWordState due = ReviewedState(_userId, Now.AddDays(-3));
        GivenDue([due], dueCount: 1);

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        // 1 due + 10 new (default cap) = 11 planned items.
        _issues.Verify(
            i => i.AddAsync(
                It.Is<VocabularySessionIssue>(x =>
                    x.SessionId == result.Value.SessionId && x.UserId == _userId && x.IssuedAtUtc == Now && x.PlannedCount == 11),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _states.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private VocabularySessionIssue OpenIssue(params (Guid SenseId, bool IsNew)[] items) =>
        VocabularySessionIssue.Create(Guid.NewGuid(), _userId, Now.AddMinutes(-5), items, 30, Now.AddHours(4));

    private void GivenOpen(VocabularySessionIssue issue, params Guid[] answered)
    {
        _issues
            .Setup(i => i.GetOpenAsync(_userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(issue));
        _issues
            .Setup(i => i.GetAnsweredSenseIdsAsync(_userId, issue.SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlySet<Guid>>(answered.ToHashSet()));
        _states
            .Setup(s => s.GetActiveBySenseIdsAsync(_userId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                Result.Success<IReadOnlyList<LearnerWordState>>(
                    ids.Select(id => LearnerWordState.CreateNew(Guid.NewGuid(), _userId, id, Now)).ToList()));
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_TwoGetsReturnSameSessionIdAndStoreOneRow()
    {
        VocabularySessionIssue? stored = null;
        _issues
            .Setup(i => i.AddAsync(It.IsAny<VocabularySessionIssue>(), It.IsAny<CancellationToken>()))
            .Callback((VocabularySessionIssue issue, CancellationToken _) => stored = issue)
            .ReturnsAsync(Result.Success());
        GetVocabularySessionQueryHandler handler = CreateHandler();

        Result<VocabularySessionDto> first = await handler.HandleAsync(new GetVocabularySessionQuery(_userId, null));
        GivenOpen(stored!);
        Result<VocabularySessionDto> second = await handler.HandleAsync(new GetVocabularySessionQuery(_userId, null));

        second.Value.SessionId.Should().Be(first.Value.SessionId);
        second.Value.NewItems.Should().HaveCount(first.Value.NewItems.Count);
        _issues.Verify(i => i.AddAsync(It.IsAny<VocabularySessionIssue>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_NoOpenSessionAfterExpiryCreatesNewSession()
    {
        // The repository returns NotFound once the session expired (ExpiresAtUtc <= now).
        GetVocabularySessionQueryHandler handler = CreateHandler();

        Result<VocabularySessionDto> first = await handler.HandleAsync(new GetVocabularySessionQuery(_userId, null));
        Result<VocabularySessionDto> second = await handler.HandleAsync(new GetVocabularySessionQuery(_userId, null));

        second.Value.SessionId.Should().NotBe(first.Value.SessionId);
        _issues.Verify(i => i.AddAsync(It.IsAny<VocabularySessionIssue>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_AllAnsweredEndsSessionAndCreatesNew()
    {
        Guid dueSense = Guid.NewGuid();
        Guid newSense = Guid.NewGuid();
        VocabularySessionIssue old = OpenIssue((dueSense, false), (newSense, true));
        GivenOpen(old, dueSense, newSense);

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        old.EndedAtUtc.Should().Be(Now);
        result.Value.SessionId.Should().NotBe(old.SessionId);
        _issues.Verify(i => i.AddAsync(It.IsAny<VocabularySessionIssue>(), It.IsAny<CancellationToken>()), Times.Once);
        _states.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_ResumeReturnsOnlyUnansweredItemsInOrder()
    {
        Guid dueDone = Guid.NewGuid();
        Guid dueLeft = Guid.NewGuid();
        Guid newDone = Guid.NewGuid();
        Guid newLeft = Guid.NewGuid();
        VocabularySessionIssue open = OpenIssue((dueDone, false), (dueLeft, false), (newDone, true), (newLeft, true));
        GivenOpen(open, dueDone, newDone);

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        result.Value.SessionId.Should().Be(open.SessionId);
        result.Value.DueItems.Select(i => i.SenseId).Should().Equal(dueLeft);
        result.Value.NewItems.Select(i => i.SenseId).Should().Equal(newLeft);
        _issues.Verify(i => i.AddAsync(It.IsAny<VocabularySessionIssue>(), It.IsAny<CancellationToken>()), Times.Never);
        open.EndedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_EmptySessionWritesNoIssue()
    {
        _states
            .Setup(s => s.GetNewInAddedOrderAsync(_userId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<LearnerWordState>>([]));

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        result.IsSuccess.Should().BeTrue();
        _issues.Verify(i => i.AddAsync(It.IsAny<VocabularySessionIssue>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_DueFirstThenNewUpToCap()
    {
        LearnerWordState due = ReviewedState(_userId, Now.AddDays(-3));
        GivenDue([due], dueCount: 1);

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().NotBeEmpty();
        result.Value.DueItems.Should().ContainSingle().Which.SenseId.Should().Be(due.SenseId);
        result.Value.NewWordCap.Should().Be(10);
        result.Value.NewItems.Should().HaveCount(10).And.OnlyContain(i => i.Status == WordStatus.New);
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_CapMinusTodaysNewWords()
    {
        GivenIntroducedToday(7);

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        result.Value.NewWordsIntroducedToday.Should().Be(7);
        result.Value.NewItems.Should().HaveCount(3);
        _states.Verify(s => s.GetNewInAddedOrderAsync(_userId, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_TodayOverCapGivesNoNewWords()
    {
        GivenIntroducedToday(12);

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        result.Value.NewItems.Should().BeEmpty();
    }

    [Theory]
    [InlineData(25, 8)]
    [InlineData(61, 5)]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_BacklogLowersCap(int backlog, int expectedCap)
    {
        GivenDue([], dueCount: backlog);

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        result.Value.NewWordCap.Should().Be(expectedCap);
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_UserSettingOverridesCap()
    {
        Result<VocabularyLearnerSettings> settings = VocabularyLearnerSettings.Create(_userId, 2);
        _settings.Setup(s => s.GetAsync(_userId, It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        GivenDue([], dueCount: 100);

        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        result.Value.NewWordCap.Should().Be(2);
        result.Value.NewItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_ClientOffsetSetsTodayRange()
    {
        await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, "2026-10-08T03:00:00+07:00"));

        // Client day 2026-10-08 at +07:00 = [2026-10-07 17:00Z, 2026-10-08 17:00Z).
        _states.Verify(s => s.CountFirstReviewedBetweenAsync(
            _userId,
            new DateTime(2026, 10, 7, 17, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 8, 17, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_MissingHeaderUsesUtcDay()
    {
        await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, null));

        _states.Verify(s => s.CountFirstReviewedBetweenAsync(
            _userId,
            new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-10-07T20:00:00")]
    [InlineData("2026-10-07T08:00:00-13:00")]
    [InlineData("2026-10-09T21:00:00+00:00")]
    public async Task GetVocabularySessionQueryHandler_HandleAsync_InvalidHeaderIsValidationError(string header)
    {
        Result<VocabularySessionDto> result = await CreateHandler().HandleAsync(new GetVocabularySessionQuery(_userId, header));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ClientDateTime.Invalid");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
