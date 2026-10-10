using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.RecordVocabularyReview;

/// <summary>
/// Checks the raw answer against the stored exercise, takes the response time from the server clock
/// (the client value only if it is plausible), grades, reschedules the word only on the first attempt
/// of a new or due word, and writes exactly one <see cref="ReviewLog"/> — all in one save.
/// Logs ids and counts only; the answer text is never logged or stored.
/// </summary>
public sealed class RecordVocabularyReviewCommandHandler : ICommandHandler<RecordVocabularyReviewCommand, VocabularyReviewResultDto>
{
    private readonly ILearnerWordStateRepository _states;
    private readonly IReviewLogRepository _reviewLogs;
    private readonly IVocabularyExerciseRepository _exercises;
    private readonly AnswerGrader _grader;
    private readonly ResponseTimeEvaluator _timeEvaluator;
    private readonly IFsrsScheduler _scheduler;
    private readonly VocabularySchedulingOptions _schedulingOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<RecordVocabularyReviewCommand> _validator;
    private readonly ILogger<RecordVocabularyReviewCommandHandler> _logger;

    public RecordVocabularyReviewCommandHandler(
        ILearnerWordStateRepository states,
        IReviewLogRepository reviewLogs,
        IVocabularyExerciseRepository exercises,
        AnswerGrader grader,
        ResponseTimeEvaluator timeEvaluator,
        IFsrsScheduler scheduler,
        VocabularySchedulingOptions schedulingOptions,
        TimeProvider timeProvider,
        IValidator<RecordVocabularyReviewCommand> validator,
        ILogger<RecordVocabularyReviewCommandHandler> logger)
    {
        _states = states;
        _reviewLogs = reviewLogs;
        _exercises = exercises;
        _grader = grader;
        _timeEvaluator = timeEvaluator;
        _scheduler = scheduler;
        _schedulingOptions = schedulingOptions;
        _timeProvider = timeProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VocabularyReviewResultDto>> HandleAsync(RecordVocabularyReviewCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RecordVocabularyReviewCommand started: UserId={UserId}, ExerciseId={ExerciseId}",
            command.UserId, command.ExerciseId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RecordVocabularyReviewCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<VocabularyReviewResultDto>(Error.Validation("RecordVocabularyReview.Validation", validation.ToString()));
        }

        Result<VocabularyExercise> exerciseResult = await _exercises.GetTrackedAsync(command.ExerciseId, ct);
        if (exerciseResult.IsFailure && exerciseResult.Error.Type != ErrorType.NotFound)
        {
            return Result.Failure<VocabularyReviewResultDto>(exerciseResult.Error);
        }

        // Another user's exercise looks the same as a missing one.
        if (exerciseResult.IsFailure || exerciseResult.Value.UserId != command.UserId)
        {
            _logger.LogWarning(
                "RecordVocabularyReviewCommand exercise not found: UserId={UserId}, ExerciseId={ExerciseId}",
                command.UserId, command.ExerciseId);
            return Result.Failure<VocabularyReviewResultDto>(
                Error.NotFound("Exercise.NotFound", "The exercise was not found."));
        }

        VocabularyExercise exercise = exerciseResult.Value;
        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        Result answerGate = exercise.Answer(nowUtc);
        if (answerGate.IsFailure)
        {
            _logger.LogWarning(
                "RecordVocabularyReviewCommand exercise already answered: UserId={UserId}, ExerciseId={ExerciseId}",
                command.UserId, command.ExerciseId);
            return Result.Failure<VocabularyReviewResultDto>(answerGate.Error);
        }

        Result<LearnerWordState> stateResult = await _states.GetTrackedAsync(command.UserId, exercise.SenseId, ct);
        if (stateResult.IsFailure && stateResult.Error.Type != ErrorType.NotFound)
        {
            return Result.Failure<VocabularyReviewResultDto>(stateResult.Error);
        }

        if (stateResult.IsFailure || !stateResult.Value.IsActive)
        {
            _logger.LogWarning(
                "RecordVocabularyReviewCommand word not in list: UserId={UserId}, SenseId={SenseId}",
                command.UserId, exercise.SenseId);
            return Result.Failure<VocabularyReviewResultDto>(
                Error.NotFound("Review.WordNotInList", "The word is not in the learner's list."));
        }

        LearnerWordState state = stateResult.Value;

        Result<int> attemptsResult = await _reviewLogs.CountAttemptsAsync(command.UserId, exercise.SessionId, exercise.SenseId, ct);
        if (attemptsResult.IsFailure)
        {
            return Result.Failure<VocabularyReviewResultDto>(attemptsResult.Error);
        }

        int attemptNo = attemptsResult.Value + 1;
        bool isDue = state.Status == WordStatus.New || state.DueAtUtc <= nowUtc;

        bool isCorrect = AnswerChecker.IsCorrect(exercise.ExerciseType, exercise.ExpectedAnswer, command.Answer.OptionKey, command.Answer.Text);
        ResponseTiming timing = _timeEvaluator.Evaluate(command.ClientResponseMs, exercise.IssuedAtUtc, nowUtc);
        FsrsRating rating = _grader.Grade(exercise.ExerciseType, command.AgeGroup, isCorrect, timing.UsedMs, command.HintUsed);
        bool reschedule = attemptNo == 1 && isDue;

        if (reschedule)
        {
            state.ApplyReview(rating, nowUtc, _scheduler, _schedulingOptions);
        }

        ReviewLog log = ReviewLog.Create(
            Guid.NewGuid(),
            command.UserId,
            exercise.SenseId,
            exercise.SessionId,
            nowUtc,
            exercise.ExerciseType,
            exercise.Skill,
            isCorrect,
            timing.UsedMs,
            command.HintUsed,
            isDue,
            attemptNo,
            rating,
            exercise.ExerciseId,
            timing.ClientMs,
            timing.ServerMs,
            timing.Adjusted);

        Result addResult = await _reviewLogs.AddAsync(log, ct);
        if (addResult.IsFailure)
        {
            return Result.Failure<VocabularyReviewResultDto>(addResult.Error);
        }

        Result saveResult = await _states.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "RecordVocabularyReviewCommand failed to save: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return Result.Failure<VocabularyReviewResultDto>(saveResult.Error);
        }

        _logger.LogInformation(
            "RecordVocabularyReviewCommand succeeded: UserId={UserId}, SenseId={SenseId}, AttemptNo={AttemptNo}, IsDue={IsDue}, Rescheduled={Rescheduled}, IsCorrect={IsCorrect}, TimingAdjusted={TimingAdjusted}",
            command.UserId, exercise.SenseId, attemptNo, isDue, reschedule, isCorrect, timing.Adjusted);

        return Result.Success(new VocabularyReviewResultDto(state.Status, state.DueAtUtc, rating, isCorrect, exercise.CorrectWord));
    }
}
