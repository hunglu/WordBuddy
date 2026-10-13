using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryHandler : IQueryHandler<GetAdminLessonsQuery, IReadOnlyList<AdminLessonDto>>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<GetAdminLessonsQueryHandler> _logger;

    public GetAdminLessonsQueryHandler(ILessonRepository lessonRepository, ILogger<GetAdminLessonsQueryHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AdminLessonDto>>> HandleAsync(GetAdminLessonsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetAdminLessonsQuery started: Type={Type}, Level={Level}, IsPublished={IsPublished}",
            query.Type, query.Level, query.IsPublished);

        Result<IReadOnlyList<Lesson>> lessonsResult = await _lessonRepository.ListAllAsync(query.Type, query.Level, query.IsPublished, ct);
        if (lessonsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetAdminLessonsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                lessonsResult.Error.Code, lessonsResult.Error.Description);
            return Result.Failure<IReadOnlyList<AdminLessonDto>>(lessonsResult.Error);
        }

        IReadOnlyList<AdminLessonDto> dtos = lessonsResult.Value
            .Select(l => new AdminLessonDto(
                l.Id, l.Title, l.Description, l.Type, l.Level, l.TargetAgeGroup, l.IsPublished, l.VocabularyWords.Count))
            .ToList();

        _logger.LogInformation("GetAdminLessonsQuery succeeded: Count={Count}", dtos.Count);
        return Result.Success(dtos);
    }
}
