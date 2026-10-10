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
    private const string RightKey = "key-right";
    private const string WrongKey = "key-wrong";

    private static readonly DateTime Now = new(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILearnerWordStateRepository> _states = new();
    private readonly Mock<IReviewLogRepository> _reviewLogs = new();
    private readonly Mock<IVocabularyExerciseRepository> _exercises = new();
    private readonly List<ReviewLog> _added = [];
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _senseId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();
    private VocabularyExercise _exercise = null!;

    public RecordVocabularyReviewCommandHandlerTests()
    {
        _reviewLogs
            .Setup(r => r.AddAsync(It.IsAny<ReviewLog>(), It.IsAny<CancellationToken>()))
            .Callback<ReviewLog, CancellationToken>((log, _) => _added.Add(log))
            .ReturnsAsync(Result.Success());
        _states.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        GivenExercise(issuedSecondsAgo: 5);
    }

    private RecordVocabularyReviewCommandHandler CreateHandler()
    {
        VocabularySchedulingOptions options = new();
        VocabularyGradingOptions grading = new();
        return new RecordVocabularyReviewCommandHandler(
            _states.Object,
            _reviewLogs.Object,
            _exercises.Object,
            new AnswerGrader(grading),
            new ResponseTimeEvaluator(grading),
            new FsrsScheduler(options),
            options,
            new FixedTimeProvider(Now),
            new RecordVocabularyReviewCommandValidator(),
            Mock.Of<ILogger<RecordVocabularyReviewCommandHandler>>());
    }

    private void GivenExercise(double issuedSecondsAgo, ExerciseType type = ExerciseType.PictureChoice, Guid? ownerId = null)
    {
        Dictionary<string, Guid> options = type == ExerciseType.Typing
            ? []
            : new() { [RightKey] = _senseId, [WrongKey] = Guid.NewGuid() };
        _exercise = VocabularyExercise.Create(
            Guid.NewGuid(), ownerId ?? _userId, _sessionId, _senseId, type,
            type == ExerciseType.Typing ? VocabularySkill.Spelling : VocabularySkill.Meaning,
            type == ExerciseType.Typing ? "apple" : RightKey, "Apple", options, Now.AddSeconds(-issuedSecondsAgo));
        _exercises
            .Setup(r => r.GetTrackedAsync(_exercise.ExerciseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(_exercise));
    }

    private RecordVocabularyReviewCommand Command(
        string? optionKey = RightKey, string? text = null, int clientMs = 5000, AgeGroup ageGroup = AgeGroup.Adult, bool hint = false) =>
        new(_userId, ageGroup, _exercise.ExerciseId, new ReviewAnswer(optionKey, text), clientMs, hint);

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
        result.Value.IsCorrect.Should().BeTrue();
        result.Value.CorrectAnswer.Should().Be("Apple");
        state.Reps.Should().Be(1);
        state.FirstReviewedAtUtc.Should().Be(Now);

        _added.Should().ContainSingle();
        ReviewLog log = _added[0];
        log.AttemptNo.Should().Be(1);
        log.IsDue.Should().BeTrue();
        log.IsCorrect.Should().BeTrue();
        log.Rating.Should().Be(FsrsRating.Good);
        log.OccurredAtUtc.Should().Be(Now);
        log.ExerciseId.Should().Be(_exercise.ExerciseId);
        log.SenseId.Should().Be(_senseId);
        log.SessionId.Should().Be(_sessionId);
        log.ExerciseType.Should().Be(ExerciseType.PictureChoice);
        log.Skill.Should().Be(VocabularySkill.Meaning);
        log.ClientResponseMs.Should().Be(5000);
        log.ServerResponseMs.Should().Be(5000);
        log.TimingAdjusted.Should().BeFalse();
        _exercise.AnsweredAtUtc.Should().Be(Now);
        _states.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_WrongKeyGivesAgain()
    {
        LearnerWordState state = GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(optionKey: WrongKey));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrect.Should().BeFalse();
        result.Value.Rating.Should().Be(FsrsRating.Again);
        result.Value.CorrectAnswer.Should().Be("Apple");
        _added.Should().ContainSingle().Which.IsCorrect.Should().BeFalse();
        state.Reps.Should().Be(1, "the first attempt still reschedules, with the lowest rating");
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_TypingWrongTextGivesAgainAndTypingRightTextIsCorrect()
    {
        GivenExercise(issuedSecondsAgo: 5, ExerciseType.Typing);
        GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);

        Result<VocabularyReviewResultDto> wrong = await CreateHandler().HandleAsync(Command(optionKey: null, text: "aple"));
        wrong.Value.IsCorrect.Should().BeFalse();
        wrong.Value.Rating.Should().Be(FsrsRating.Again);

        GivenExercise(issuedSecondsAgo: 5, ExerciseType.Typing);
        GivenPriorAttempts(1);
        Result<VocabularyReviewResultDto> right = await CreateHandler().HandleAsync(Command(optionKey: null, text: "  APPLE "));
        right.Value.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_ForgedFastClientTimeIsAdjustedAndLowersRating()
    {
        // Issued 12 s ago. A client claiming 500 ms is implausible: used time = 12000 - 3000 = 9000 ms.
        GivenExercise(issuedSecondsAgo: 12);
        GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);

        Result<VocabularyReviewResultDto> forged = await CreateHandler().HandleAsync(Command(clientMs: 500));

        // 9000 ms on PictureChoice is Good (fast <= 3000). Trusting 500 ms would have been Easy.
        forged.Value.Rating.Should().Be(FsrsRating.Good);
        ReviewLog log = _added.Should().ContainSingle().Subject;
        log.TimingAdjusted.Should().BeTrue();
        log.ClientResponseMs.Should().Be(500);
        log.ServerResponseMs.Should().Be(12000);
        log.ResponseMs.Should().Be(9000);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_HonestSlowClientTimeIsKept()
    {
        GivenExercise(issuedSecondsAgo: 12);
        GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(clientMs: 11500));

        result.Value.Rating.Should().Be(FsrsRating.Hard, "11.5 s is above the 10 s slow threshold");
        _added.Single().TimingAdjusted.Should().BeFalse();
    }

    [Theory]
    [InlineData(AgeGroup.Adult, FsrsRating.Good)]
    [InlineData(AgeGroup.Child, FsrsRating.Easy)]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_ChildGetsLenientThresholds(AgeGroup ageGroup, FsrsRating expected)
    {
        // 3500 ms on PictureChoice: adult Good (> 3000), child Easy (<= 3750).
        GivenExercise(issuedSecondsAgo: 3.5);
        GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(clientMs: 3500, ageGroup: ageGroup));

        result.Value.Rating.Should().Be(expected);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_AlreadyAnsweredReturnsConflictAndWritesNothing()
    {
        GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);
        _exercise.Answer(Now.AddSeconds(-1));

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Exercise.AlreadyAnswered");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        _added.Should().BeEmpty();
        _states.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_ForeignExerciseIsNotFound()
    {
        GivenExercise(issuedSecondsAgo: 5, ownerId: Guid.NewGuid());
        GivenState(Now.AddDays(-1));

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command());

        result.Error.Code.Should().Be("Exercise.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _exercise.AnsweredAtUtc.Should().BeNull();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_UnknownExerciseIsNotFound()
    {
        _exercises
            .Setup(r => r.GetTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<VocabularyExercise>(Error.NotFound("Exercise.NotFound", "none")));

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command());

        result.Error.Code.Should().Be("Exercise.NotFound");
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_RetryInSameSessionOnlyLogs()
    {
        LearnerWordState state = GivenState(Now.AddDays(-1));
        GivenPriorAttempts(1);

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(optionKey: WrongKey));

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

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command());

        result.Error.Code.Should().Be("Review.WordNotInList");
        _added.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(600_001)]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_ClientResponseMsOutOfRangeFailsValidation(int clientMs)
    {
        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(clientMs: clientMs));

        result.Error.Code.Should().Be("RecordVocabularyReview.Validation");
        _states.VerifyNoOtherCalls();
        _exercises.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_EmptyAnswerFailsValidation()
    {
        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command(optionKey: null, text: null));

        result.Error.Code.Should().Be("RecordVocabularyReview.Validation");
        _exercises.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RecordVocabularyReviewCommandHandler_HandleAsync_ConcurrentDuplicateReturnsConflict()
    {
        GivenState(Now.AddDays(-1));
        GivenPriorAttempts(0);
        _states
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("LearnerWordState.ConcurrentUpdate", "changed")));

        Result<VocabularyReviewResultDto> result = await CreateHandler().HandleAsync(Command());

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("LearnerWordState.ConcurrentUpdate");
    }
}
