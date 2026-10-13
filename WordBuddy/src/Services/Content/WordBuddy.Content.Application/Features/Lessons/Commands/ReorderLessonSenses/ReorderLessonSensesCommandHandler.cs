using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.ReorderLessonSenses;

public sealed class ReorderLessonSensesCommandHandler : ICommandHandler<ReorderLessonSensesCommand>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<ReorderLessonSensesCommand> _validator;
    private readonly ILogger<ReorderLessonSensesCommandHandler> _logger;

    public ReorderLessonSensesCommandHandler(
        ILessonRepository lessonRepository,
        IValidator<ReorderLessonSensesCommand> validator,
        ILogger<ReorderLessonSensesCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(ReorderLessonSensesCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("ReorderLessonSensesCommand started: LessonId={LessonId}", command.LessonId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("ReorderLessonSensesCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("ReorderLessonSenses.Validation", validation.ToString()));
        }

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdForUpdateAsync(command.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "ReorderLessonSensesCommand lesson lookup failed: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure(lessonResult.Error);
        }

        Result reorderResult = lessonResult.Value.ReorderSenses(command.SenseIds);
        if (reorderResult.IsFailure)
        {
            _logger.LogWarning(
                "ReorderLessonSensesCommand rejected by lesson: {ErrorCode} — {ErrorDescription}",
                reorderResult.Error.Code, reorderResult.Error.Description);
            return reorderResult;
        }

        Result saveResult = await _lessonRepository.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "ReorderLessonSensesCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation("ReorderLessonSensesCommand succeeded: LessonId={LessonId}", command.LessonId);
        return Result.Success();
    }
}
