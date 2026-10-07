using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.VocabularySrs;

public class RecordVocabularyReviewCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILearnerWordStateRepository> _states = new();
    private readonly Mock<IReviewLogRepository> _reviewLogs = new();
    private readonly List<ReviewLog> _added = [];
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _senseId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();

    public RecordVocabularyReviewCommandHandlerTests()
    {
        _reviewLogs
            .Setup(r => r.AddAsync(It.IsAny<ReviewLog>(), It.IsAny<CancellationToken>()))
            .Callback<ReviewLog, CancellationToken>((log, _) => _added.Add(log))
            .ReturnsAsync(Result.Success());
        _states.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private RecordVocabularyReviewCommandHandler CreateHandler()
    {
        VocabularySchedulingOptions options = new();
        return new RecordVocabularyReviewCommandHandler(
            _states.Object,
            _reviewLogs.Object,
            new AnswerGrader(new VocabularyGradingOptions()),
            new FsrsScheduler(options),
            options,
            new FixedTimeProvider(Now),
            new RecordVocabularyReviewCommandValidator(),
            Mock.Of<ILogger<RecordVocabularyReviewCommandHandler>>());
    }

    private RecordVocabularyReviewCommand Command(
        bool isCorrect = true, int responseMs = 5000, AgeGroup ageGroup = AgeGroup.Adult, Guid? senseId = null) =>
        new(_userId, ageGroup, _sessionId, senseId ?? _senseId, ExerciseType.PictureChoice, VocabularySkill.Meaning,
            isCorrect, responseMs, HintUsed: false);

    private LearnerWordState GivenState(DateTime addedAtUtc)
    {
        LearnerWordState state = LearnerWordState.CreateNew(Guid.NewGuid(), _userId, _senseId, addedAtUtc);
        _states.Setup(r => r.GetTrackedAsync(_userId, _senseId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(state));
        return state;
    }

    private void GivenPriorAttempts(int count) =>
        _reviewLogs
            .Setup(r => r.CountAttemptsAsync(_userId, _sessionId, _senseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(count));

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_FirstAttemptOnNewWordReschedules()
    {
        LearnerWordState state = GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command());

        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(FsrsRating.Good);
        result.Value.Status.Should().Be(WordStatus.Learning);
        state.Reps.Should().Be(1);
        state.FirstReviewedAtUtc.Should().Be(Now);

        _added.Should().ContainSingle();
        ReviewLog log = _added[0];
        log.AttemptNo.Should().Be(1);
        log.IsDue.Should().BeTrue();
        log.Rating.Should().Be(FsrsRating.Good);
        log.OccurredAtUtc.Should().Be(Now);
        _states.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_RetryInSameSessionOnlyLogs()
    {
        LearnerWordState state = GivenState(Now.AddDays(-1));
        GivenPriorAttempts(1);

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(isCorrect: false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(FsrsRating.Again);
        state.Reps.Should().Be(0);
        state.Status.Should().Be(WordStatus.New);
        _added.Should().ContainSingle().Which.AttemptNo.Should().Be(2);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_NotDueWordOnlyLogs()
    {
        LearnerWordState state = GivenState(Now.AddDays(-1));
        state.ApplyReview(FsrsRating.Easy, Now.AddHours(-1), new FsrsScheduler(new VocabularySchedulingOptions()), new VocabularySchedulingOptions());
        DateTime dueBefore = state.DueAtUtc;
        GivenPriorAttempts(0);

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command());

        result.IsSuccess.Should().BeTrue();
        state.Reps.Should().Be(1);
        state.DueAtUtc.Should().Be(dueBefore);
        ReviewLog log = _added.Should().ContainSingle().Subject;
        log.IsDue.Should().BeFalse();
        log.AttemptNo.Should().Be(1);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_ChildGetsLenientThresholds()
    {
        GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);

        // 3500 ms on PictureChoice: adult Good (> 3000), child Easy (≤ 3750).
        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(responseMs: 3500, ageGroup: AgeGroup.Child));

        result.Value.Rating.Should().Be(FsrsRating.Easy);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_InactiveWordIsNotFound()
    {
        LearnerWordState state = GivenState(Now.AddDays(-1));
        state.Deactivate();

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Review.WordNotInList");
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _added.Should().BeEmpty();
        _states.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_UnknownWordIsNotFound()
    {
        _states
            .Setup(r => r.GetTrackedAsync(_userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LearnerWordState>(Error.NotFound("LearnerWordState.NotFound", "none")));

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(senseId: Guid.NewGuid()));

        result.Error.Code.Should().Be("Review.WordNotInList");
        _added.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(600_001)]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_ResponseMsOutOfRangeFailsValidation(int responseMs)
    {
        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(responseMs: responseMs));

        result.Error.Code.Should().Be("RecordVocabularyReview.Validation");
        _states.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_UndefinedEnumFailsValidation()
    {
        RecordVocabularyReviewCommand command = Command() with { ExerciseType = (ExerciseType)99 };

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(command);

        result.Error.Code.Should().Be("RecordVocabularyReview.Validation");
    }
}
