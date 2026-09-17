using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Progress.Commands.RecordProgress;

public sealed class RecordProgressCommandHandler : ICommandHandler<RecordProgressCommand>
{
    private readonly ILearnerProgressRepository _progressRepository;
    private readonly IValidator<RecordProgressCommand> _validator;
    private readonly ILogger<RecordProgressCommandHandler> _logger;

    public RecordProgressCommandHandler(
        ILearnerProgressRepository progressRepository,
        IValidator<RecordProgressCommand> validator,
        ILogger<RecordProgressCommandHandler> logger)
    {
        _progressRepository = progressRepository;
        _validator = validator;
        _logger = logger;
    }

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

        Result<LearnerProgress> existingResult = await _progressRepository.GetTrackedByUserAndLessonAsync(command.UserId, command.LessonId, ct);

        if (existingResult.IsSuccess)
        {
            existingResult.Value.RecordCompletion(command.IsCompleted, command.ScorePercent);
            Result saveResult = await _progressRepository.SaveChangesAsync(ct);
            if (saveResult.IsFailure)
            {
                _logger.LogWarning(
                    "RecordProgressCommand failed to update: {ErrorCode} — {ErrorDescription}",
                    saveResult.Error.Code, saveResult.Error.Description);
                return saveResult;
            }

            _logger.LogInformation("RecordProgressCommand succeeded (update): UserId={UserId}, LessonId={LessonId}", command.UserId, command.LessonId);
            return Result.Success();
        }

        LearnerProgress progress = new(Guid.NewGuid(), command.UserId, command.LessonId);
        progress.RecordCompletion(command.IsCompleted, command.ScorePercent);

        Result addResult = await _progressRepository.AddAsync(progress, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "RecordProgressCommand failed to add: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return addResult;
        }

        _logger.LogInformation("RecordProgressCommand succeeded (create): UserId={UserId}, LessonId={LessonId}", command.UserId, command.LessonId);
        return Result.Success();
    }
}
