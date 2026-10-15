using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryHandler : IQueryHandler<GetAdminLessonsQuery, PagedResult<AdminLessonDto>>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IValidator<GetAdminLessonsQuery> _validator;
    private readonly ILogger<GetAdminLessonsQueryHandler> _logger;

    public GetAdminLessonsQueryHandler(
        ILessonRepository lessonRepository,
        IValidator<GetAdminLessonsQuery> validator,
        ILogger<GetAdminLessonsQueryHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AdminLessonDto>>> HandleAsync(GetAdminLessonsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetAdminLessonsQuery started: Type={Type}, Level={Level}, IsPublished={IsPublished}, Page={Page}, PageSize={PageSize}",
            query.Type, query.Level, query.IsPublished, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetAdminLessonsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<AdminLessonDto>>(Error.Validation("GetAdminLessons.Validation", validation.ToString()));
        }

        Result<PagedResult<Lesson>> lessonsResult = await _lessonRepository.ListAllAsync(
            query.Type, query.Level, query.IsPublished, query.Page, query.PageSize, ct);
        if (lessonsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetAdminLessonsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                lessonsResult.Error.Code, lessonsResult.Error.Description);
            return Result.Failure<PagedResult<AdminLessonDto>>(lessonsResult.Error);
        }

        PagedResult<Lesson> lessons = lessonsResult.Value;
        List<AdminLessonDto> dtos = lessons.Items
            .Select(l => new AdminLessonDto(
                l.Id, l.Title, l.Description, l.Type, l.Level, l.TargetAgeGroup, l.IsPublished, l.VocabularyWords.Count))
            .ToList();

        _logger.LogInformation("GetAdminLessonsQuery succeeded: Count={Count}, TotalCount={TotalCount}", dtos.Count, lessons.TotalCount);
        return Result.Success(new PagedResult<AdminLessonDto>(dtos, lessons.Page, lessons.PageSize, lessons.TotalCount));
    }
}
