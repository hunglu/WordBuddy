using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.UnpublishLesson;

public sealed class UnpublishLessonCommandHandler : ICommandHandler<UnpublishLessonCommand>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<UnpublishLessonCommand> _validator;
    private readonly ILogger<UnpublishLessonCommandHandler> _logger;

    public UnpublishLessonCommandHandler(
        ILessonRepository lessonRepository,
        IValidator<UnpublishLessonCommand> validator,
        ILogger<UnpublishLessonCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(UnpublishLessonCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("UnpublishLessonCommand started: LessonId={LessonId}", command.LessonId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("UnpublishLessonCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("UnpublishLesson.Validation", validation.ToString()));
        }

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdForUpdateAsync(command.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "UnpublishLessonCommand lesson lookup failed: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure(lessonResult.Error);
        }

        Result changeResult = lessonResult.Value.Unpublish();
        if (changeResult.IsFailure)
        {
            _logger.LogWarning(
                "UnpublishLessonCommand rejected by lesson: {ErrorCode} — {ErrorDescription}",
                changeResult.Error.Code, changeResult.Error.Description);
            return changeResult;
        }

        Result saveResult = await _lessonRepository.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "UnpublishLessonCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation("UnpublishLessonCommand succeeded: LessonId={LessonId}", command.LessonId);
        return Result.Success();
    }
}
