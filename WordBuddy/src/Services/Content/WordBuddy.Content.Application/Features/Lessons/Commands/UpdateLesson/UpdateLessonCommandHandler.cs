using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.UpdateLesson;

public sealed class UpdateLessonCommandHandler : ICommandHandler<UpdateLessonCommand>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<UpdateLessonCommand> _validator;
    private readonly ILogger<UpdateLessonCommandHandler> _logger;

    public UpdateLessonCommandHandler(
        ILessonRepository lessonRepository,
        IValidator<UpdateLessonCommand> validator,
        ILogger<UpdateLessonCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(UpdateLessonCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("UpdateLessonCommand started: LessonId={LessonId}", command.LessonId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UpdateLessonCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("UpdateLesson.Validation", validation.ToString()));
        }

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdForUpdateAsync(command.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "UpdateLessonCommand lesson lookup failed: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure(lessonResult.Error);
        }

        Result updateResult = lessonResult.Value.UpdateDetails(command.Title, command.Description, command.Level, command.TargetAgeGroup);
        if (updateResult.IsFailure)
        {
            _logger.LogWarning(
                "UpdateLessonCommand rejected by lesson: {ErrorCode} — {ErrorDescription}",
                updateResult.Error.Code, updateResult.Error.Description);
            return updateResult;
        }

        Result saveResult = await _lessonRepository.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "UpdateLessonCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation("UpdateLessonCommand succeeded: LessonId={LessonId}", command.LessonId);
        return Result.Success();
    }
}
