using FluentValidation;
using FluentValidation.Results;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Features.Progress.Commands.RecordProgress;

/// <summary>Upserts a learner progress record: creates on first access, updates on subsequent calls.</summary>
public sealed class RecordProgressCommandHandler
{
    private readonly ILearnerProgressRepository _progressRepository;
    private readonly IValidator<RecordProgressCommand> _validator;

    /// <summary>Initializes a new <see cref="RecordProgressCommandHandler"/>.</summary>
    public RecordProgressCommandHandler(
        ILearnerProgressRepository progressRepository,
        IValidator<RecordProgressCommand> validator)
    {
        _progressRepository = progressRepository;
        _validator = validator;
    }

    /// <summary>Handles the command, upserting the learner's progress record.</summary>
    public async Task<Result> HandleAsync(RecordProgressCommand command, CancellationToken ct = default)
    {
        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result.Failure(Error.Validation("RecordProgress.Validation", validation.ToString()));

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

            return await _progressRepository.UpdateAsync(updated, ct);
        }

        // Code "LearnerProgress.NotFound" is the expected first-access path; any other code is an infrastructure error.
        if (existing.Error.Code != "LearnerProgress.NotFound")
            return Result.Failure(existing.Error);

        LearnerProgress newProgress = new(
            Guid.NewGuid(),
            command.UserId,
            command.LessonId,
            command.IsCompleted,
            command.ScorePercent,
            lastAccessedAt: now,
            completedAt: command.IsCompleted ? now : null);

        return await _progressRepository.AddAsync(newProgress, ct);
    }
}
