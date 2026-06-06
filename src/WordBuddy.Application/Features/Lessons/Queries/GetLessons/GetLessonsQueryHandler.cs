using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.Features.Lessons.Queries.GetLessons;

/// <summary>Returns filtered, published lessons sorted by display order.</summary>
public sealed class GetLessonsQueryHandler
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<GetLessonsQuery> _validator;
    private readonly ILogger<GetLessonsQueryHandler> _logger;

    /// <summary>Initializes a new <see cref="GetLessonsQueryHandler"/>.</summary>
    public GetLessonsQueryHandler(
        ILessonRepository lessonRepository,
        IValidator<GetLessonsQuery> validator,
        ILogger<GetLessonsQueryHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the query and returns a filtered list of lesson summaries.</summary>
    public async Task<Result<List<LessonDto>>> HandleAsync(GetLessonsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetLessonsQuery started: Type={Type}, Level={Level}, AgeGroup={AgeGroup}",
            query.Type, query.Level, query.AgeGroup);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetLessonsQuery validation failed: {Errors}", validation.ToString());
            return Result<List<LessonDto>>.Failure(
                Error.Validation("GetLessons.Validation", validation.ToString()));
        }

        Result<IReadOnlyList<Lesson>> lessonsResult = await _lessonRepository.GetPublishedAsync(ct);
        if (lessonsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetLessonsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                lessonsResult.Error.Code, lessonsResult.Error.Description);
            return Result<List<LessonDto>>.Failure(lessonsResult.Error);
        }

        IEnumerable<Lesson> lessons = lessonsResult.Value;

        if (query.Type.HasValue)
            lessons = lessons.Where(l => l.Type == query.Type.Value);

        if (query.Level.HasValue)
            lessons = lessons.Where(l => l.Level == query.Level.Value);

        if (query.AgeGroup.HasValue)
        {
            lessons = query.AgeGroup.Value == AgeGroup.Child
                ? lessons.Where(l => l.TargetAgeGroup == TargetAgeGroup.Child || l.TargetAgeGroup == TargetAgeGroup.Both)
                : lessons.Where(l => l.TargetAgeGroup == TargetAgeGroup.Adult || l.TargetAgeGroup == TargetAgeGroup.Both);
        }

        List<LessonDto> dtos = lessons
            .OrderBy(l => l.OrderIndex)
            .Select(ToDto)
            .ToList();

        return Result<List<LessonDto>>.Success(dtos);
    }

    private static LessonDto ToDto(Lesson l) =>
        new(l.Id, l.Title, l.Description, l.Type, l.Level,
            l.TargetAgeGroup, l.IsPublished, l.OrderIndex, l.CreatedAt);
}
