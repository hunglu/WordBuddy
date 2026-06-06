using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Features.Progress.Commands.RecordProgress;

/// <summary>Upserts a learner progress record: creates on first access, updates on subsequent calls.</summary>
public sealed class RecordProgressCommandHandler
{
    private readonly ILearnerProgressRepository _progressRepository;
    private readonly IValidator<RecordProgressCommand> _validator;
    private readonly ILogger<RecordProgressCommandHandler> _logger;

    /// <summary>Initializes a new <see cref="RecordProgressCommandHandler"/>.</summary>
    public RecordProgressCommandHandler(
        ILearnerProgressRepository progressRepository,
        IValidator<RecordProgressCommand> validator,
        ILogger<RecordProgressCommandHandler> logger)
    {
        _progressRepository = progressRepository;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the command, upserting the learner's progress record.</summary>
    public async Task<Result> HandleAsync(RecordProgressCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RecordProgressCommand started: UserId={UserId}, LessonId={LessonId}, IsCompleted={IsCompleted}",
            command.UserId, command.LessonId, command.IsCompleted);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RecordProgressCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RecordProgress.Validation", validation.ToString()));
        }

        DateTime now = DateTime.UtcNow;

        Result<LearnerProgress> existing =
            await _progressRepository.GetByUserAndLessonAsync(command.UserId, command.LessonId, ct);

        if (existing.IsSuccess)
        {
            LearnerProgress updated = new(
                existing.Value.Id,
                existing.Value.UserId,
                existing.Value.LessonId,
                command.IsCompleted,
                command.ScorePercent,
                lastAccessedAt: now,
                completedAt: command.IsCompleted ? now : existing.Value.CompletedAt);

            Result updateResult = await _progressRepository.UpdateAsync(updated, ct);
            if (updateResult.IsFailure)
            {
                _logger.LogWarning(
                    "RecordProgressCommand update failed: UserId={UserId}, LessonId={LessonId}, {ErrorCode} — {ErrorDescription}",
                    command.UserId, command.LessonId, updateResult.Error.Code, updateResult.Error.Description);
            }
            return updateResult;
        }

        // Code "LearnerProgress.NotFound" is the expected first-access path; any other code is an infrastructure error.
        if (existing.Error.Code != "LearnerProgress.NotFound")
        {
            _logger.LogWarning(
                "RecordProgressCommand unexpected infrastructure error: UserId={UserId}, LessonId={LessonId}, {ErrorCode} — {ErrorDescription}",
                command.UserId, command.LessonId, existing.Error.Code, existing.Error.Description);
            return Result.Failure(existing.Error);
        }

        LearnerProgress newProgress = new(
            Guid.NewGuid(),
            command.UserId,
            command.LessonId,
            command.IsCompleted,
            command.ScorePercent,
            lastAccessedAt: now,
            completedAt: command.IsCompleted ? now : null);

        Result addResult = await _progressRepository.AddAsync(newProgress, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "RecordProgressCommand add failed: UserId={UserId}, LessonId={LessonId}, {ErrorCode} — {ErrorDescription}",
                command.UserId, command.LessonId, addResult.Error.Code, addResult.Error.Description);
        }
        return addResult;
    }
}
