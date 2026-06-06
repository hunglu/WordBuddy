using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Features.Lessons.Commands.CreateLesson;

/// <summary>Creates and persists a new lesson, returning its generated identifier.</summary>
public sealed class CreateLessonCommandHandler
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<CreateLessonCommand> _validator;
    private readonly ILogger<CreateLessonCommandHandler> _logger;

    /// <summary>Initializes a new <see cref="CreateLessonCommandHandler"/>.</summary>
    public CreateLessonCommandHandler(
        ILessonRepository lessonRepository,
        IValidator<CreateLessonCommand> validator,
        ILogger<CreateLessonCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the command and returns the new lesson's <see cref="Guid"/> on success.</summary>
    public async Task<Result<Guid>> HandleAsync(CreateLessonCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CreateLessonCommand started: Title={Title}, Type={Type}, Level={Level}",
            command.Title, command.Type, command.Level);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateLessonCommand validation failed: {Errors}", validation.ToString());
            return Result<Guid>.Failure(
                Error.Validation("CreateLesson.Validation", validation.ToString()));
        }

        Guid id = Guid.NewGuid();

        Lesson lesson = new(
            id,
            command.Title,
            command.Description,
            command.Type,
            command.Level,
            command.TargetAgeGroup,
            isPublished: false,
            command.OrderIndex,
            createdAt: DateTime.UtcNow);

        Result addResult = await _lessonRepository.AddAsync(lesson, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "CreateLessonCommand failed to persist lesson: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result<Guid>.Failure(addResult.Error);
        }

        _logger.LogInformation("CreateLessonCommand succeeded: LessonId={LessonId}", id);
        return Result<Guid>.Success(id);
    }
}
