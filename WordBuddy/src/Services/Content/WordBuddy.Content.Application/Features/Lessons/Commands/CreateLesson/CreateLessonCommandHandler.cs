using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.CreateLesson;

public sealed class CreateLessonCommandHandler : ICommandHandler<CreateLessonCommand, Guid>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<CreateLessonCommand> _validator;
    private readonly ILogger<CreateLessonCommandHandler> _logger;

    public CreateLessonCommandHandler(
        ILessonRepository lessonRepository,
        IValidator<CreateLessonCommand> validator,
        ILogger<CreateLessonCommandHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> HandleAsync(CreateLessonCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CreateLessonCommand started: Title={Title}, Type={Type}, Level={Level}",
            command.Title, command.Type, command.Level);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateLessonCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<Guid>(Error.Validation("CreateLesson.Validation", validation.ToString()));
        }

        Guid id = Guid.NewGuid();
        Lesson lesson = new(id, command.Title, command.Description, command.Type, command.Level, command.TargetAgeGroup);

        Result addResult = await _lessonRepository.AddAsync(lesson, ct);
        if (addResult.IsFailure)
        {
            _logger.LogWarning(
                "CreateLessonCommand failed to persist lesson: {ErrorCode} — {ErrorDescription}",
                addResult.Error.Code, addResult.Error.Description);
            return Result.Failure<Guid>(addResult.Error);
        }

        _logger.LogInformation("CreateLessonCommand succeeded: LessonId={LessonId}", id);
        return Result.Success(id);
    }
}
