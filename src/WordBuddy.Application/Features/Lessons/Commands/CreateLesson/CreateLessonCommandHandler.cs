using FluentValidation;
using FluentValidation.Results;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Features.Lessons.Commands.CreateLesson;

/// <summary>Creates and persists a new lesson, returning its generated identifier.</summary>
public sealed class CreateLessonCommandHandler
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<CreateLessonCommand> _validator;

    /// <summary>Initializes a new <see cref="CreateLessonCommandHandler"/>.</summary>
    public CreateLessonCommandHandler(ILessonRepository lessonRepository, IValidator<CreateLessonCommand> validator)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
    }

    /// <summary>Handles the command and returns the new lesson's <see cref="Guid"/> on success.</summary>
    public async Task<Result<Guid>> HandleAsync(CreateLessonCommand command, CancellationToken ct = default)
    {
        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<Guid>.Failure(
                Error.Validation("CreateLesson.Validation", validation.ToString()));

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
            return Result<Guid>.Failure(addResult.Error);

        return Result<Guid>.Success(id);
    }
}
