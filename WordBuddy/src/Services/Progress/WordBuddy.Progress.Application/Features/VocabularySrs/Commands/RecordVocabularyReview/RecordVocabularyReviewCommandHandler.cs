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
/// Grades one answer, reschedules the word only on the first attempt of a new or due word, and
/// writes exactly one <see cref="ReviewLog"/> — all in one save. Logs ids and counts only.
/// </summary>
public sealed class RecordVocabularyReviewCommandHandler : ICommandHandler<RecordVocabularyReviewCommand, VocabularyReviewResultDto>
{
    private readonly ILearnerWordStateRepository _states;
    private readonly IReviewLogRepository _reviewLogs;
    private readonly AnswerGrader _grader;
    private readonly IFsrsScheduler _scheduler;
    private readonly VocabularySchedulingOptions _schedulingOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<RecordVocabularyReviewCommand> _validator;
    private readonly ILogger<RecordVocabularyReviewCommandHandler> _logger;

    public RecordVocabularyReviewCommandHandler(
        ILearnerWordStateRepository states,
        IReviewLogRepository reviewLogs,
        AnswerGrader grader,
        IFsrsScheduler scheduler,
        VocabularySchedulingOptions schedulingOptions,
        TimeProvider timeProvider,
        IValidator<RecordVocabularyReviewCommand> validator,
        ILogger<RecordVocabularyReviewCommandHandler> logger)
    {
        _states = states;
        _reviewLogs = reviewLogs;
        _grader = grader;
        _scheduler = scheduler;
        _schedulingOptions = schedulingOptions;
        _timeProvider = timeProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<VocabularyReviewResultDto>> HandleAsync(RecordVocabularyReviewCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RecordVocabularyReviewCommand started: UserId={UserId}, SessionId={SessionId}, SenseId={SenseId}",
            command.UserId, command.SessionId, command.SenseId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RecordVocabularyReviewCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<VocabularyReviewResultDto>(Error.Validation("RecordVocabularyReview.Validation", validation.ToString()));
        }

        Result<LearnerWordState> stateResult = await _states.GetTrackedAsync(command.UserId, command.SenseId, ct);
        if (stateResult.IsFailure && stateResult.Error.Type != ErrorType.NotFound)
        {
            return Result.Failure<VocabularyReviewResultDto>(stateResult.Error);
        }

        if (stateResult.IsFailure || !stateResult.Value.IsActive)
        {
            _logger.LogWarning(
                "RecordVocabularyReviewCommand word not in list: UserId={UserId}, SenseId={SenseId}",
                command.UserId, command.SenseId);
            return Result.Failure<VocabularyReviewResultDto>(
                Error.NotFound("Review.WordNotInList", "The word is not in the learner's list."));
        }

        LearnerWordState state = stateResult.Value;
        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        Result<int> attemptsResult = await _reviewLogs.CountAttemptsAsync(command.UserId, command.SessionId, command.SenseId, ct);
        if (attemptsResult.IsFailure)
        {
            return Result.Failure<VocabularyReviewResultDto>(attemptsResult.Error);
        }

        int attemptNo = attemptsResult.Value + 1;
        bool isDue = state.Status == WordStatus.New || state.DueAtUtc <= nowUtc;
        FsrsRating rating = _grader.Grade(command.ExerciseType, command.AgeGroup, command.IsCorrect, command.ResponseMs, command.HintUsed);
        bool reschedule = attemptNo == 1 && isDue;

        if (reschedule)
        {
            state.ApplyReview(rating, nowUtc, _scheduler, _schedulingOptions);
        }

        ReviewLog log = ReviewLog.Create(
            Guid.NewGuid(),
            command.UserId,
            command.SenseId,
            command.SessionId,
            nowUtc,
            command.ExerciseType,
            command.Skill,
            command.IsCorrect,
            command.ResponseMs,
            command.HintUsed,
            isDue,
            attemptNo,
            rating);

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
            "RecordVocabularyReviewCommand succeeded: UserId={UserId}, SenseId={SenseId}, AttemptNo={AttemptNo}, IsDue={IsDue}, Rescheduled={Rescheduled}",
            command.UserId, command.SenseId, attemptNo, isDue, reschedule);

        return Result.Success(new VocabularyReviewResultDto(state.Status, state.DueAtUtc, rating));
    }
}
