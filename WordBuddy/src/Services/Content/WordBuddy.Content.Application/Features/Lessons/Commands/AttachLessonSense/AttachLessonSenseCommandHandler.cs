using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.AttachLessonSense;

public sealed class AttachLessonSenseCommandHandler : ICommandHandler<AttachLessonSenseCommand>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IVocabularyWordRepository _senseRepository;
    private readonly IValidator<AttachLessonSenseCommand> _validator;
    private readonly ILogger<AttachLessonSenseCommandHandler> _logger;

    public AttachLessonSenseCommandHandler(
        ILessonRepository lessonRepository,
        IVocabularyWordRepository senseRepository,
        IValidator<AttachLessonSenseCommand> validator,
        ILogger<AttachLessonSenseCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _senseRepository = senseRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(AttachLessonSenseCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("AttachLessonSenseCommand started: LessonId={LessonId}, SenseId={SenseId}", command.LessonId, command.SenseId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AttachLessonSenseCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("AttachLessonSense.Validation", validation.ToString()));
        }

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdForUpdateAsync(command.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "AttachLessonSenseCommand lesson lookup failed: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure(lessonResult.Error);
        }

        Result<Sense> senseResult = await _senseRepository.GetByIdAsync(command.SenseId, ct);
        if (senseResult.IsFailure)
        {
            _logger.LogWarning(
                "AttachLessonSenseCommand sense lookup failed: {ErrorCode} — {ErrorDescription}",
                senseResult.Error.Code, senseResult.Error.Description);
            return Result.Failure(Error.NotFound("Sense.NotFound", $"Sense {command.SenseId} was not found."));
        }

        // Eligibility needs the sense, so it lives here and not in the Lesson aggregate.
        Sense sense = senseResult.Value;
        bool eligible = sense.Source == VocabularySource.System || sense.ShareStatus == VocabularyShareStatus.Shared;
        if (!eligible)
        {
            _logger.LogWarning(
                "AttachLessonSenseCommand sense not eligible: SenseId={SenseId}, ShareStatus={ShareStatus}",
                sense.Id, sense.ShareStatus);
            return Result.Failure(Error.Validation(
                "Lesson.SenseNotEligible",
                $"Sense {sense.Id} must be a system word or a shared word to join a lesson."));
        }

        Result attachResult = lessonResult.Value.AttachSense(sense);
        if (attachResult.IsFailure)
        {
            _logger.LogWarning(
                "AttachLessonSenseCommand rejected by lesson: {ErrorCode} — {ErrorDescription}",
                attachResult.Error.Code, attachResult.Error.Description);
            return attachResult;
        }

        Result saveResult = await _lessonRepository.SaveChangesAsync(ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "AttachLessonSenseCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation("AttachLessonSenseCommand succeeded: LessonId={LessonId}", command.LessonId);
        return Result.Success();
    }
}
