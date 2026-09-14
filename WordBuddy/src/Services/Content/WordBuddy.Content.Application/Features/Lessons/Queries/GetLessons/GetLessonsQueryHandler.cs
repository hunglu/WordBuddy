using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetLessons;

public sealed class GetLessonsQueryHandler : IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<GetLessonsQueryHandler> _logger;

    public GetLessonsQueryHandler(ILessonRepository lessonRepository, ILogger<GetLessonsQueryHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<LessonDto>>> HandleAsync(GetLessonsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetLessonsQuery started: Type={Type}, Level={Level}", query.Type, query.Level);

        Result<IReadOnlyList<Lesson>> lessonsResult = await _lessonRepository.GetPublishedAsync(query.Type, query.Level, ct);
        if (lessonsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetLessonsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                lessonsResult.Error.Code, lessonsResult.Error.Description);
            return Result.Failure<IReadOnlyList<LessonDto>>(lessonsResult.Error);
        }

        IReadOnlyList<LessonDto> dtos = lessonsResult.Value
            .Select(l => new LessonDto(l.Id, l.Title, l.Description, l.Type, l.Level, l.TargetAgeGroup))
            .ToList();

        _logger.LogInformation("GetLessonsQuery succeeded: Count={Count}", dtos.Count);
        return Result.Success(dtos);
    }
}
