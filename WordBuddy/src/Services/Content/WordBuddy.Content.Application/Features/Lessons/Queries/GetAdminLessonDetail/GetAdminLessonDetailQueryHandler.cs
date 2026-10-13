using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessonDetail;

public sealed class GetAdminLessonDetailQueryHandler : IQueryHandler<GetAdminLessonDetailQuery, AdminLessonDetailDto>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<GetAdminLessonDetailQueryHandler> _logger;

    public GetAdminLessonDetailQueryHandler(ILessonRepository lessonRepository, ILogger<GetAdminLessonDetailQueryHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _logger = logger;
    }

    public async Task<Result<AdminLessonDetailDto>> HandleAsync(GetAdminLessonDetailQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetAdminLessonDetailQuery started: LessonId={LessonId}", query.LessonId);

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdWithDetailsAsync(query.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "GetAdminLessonDetailQuery repository failure: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure<AdminLessonDetailDto>(lessonResult.Error);
        }

        Lesson lesson = lessonResult.Value;
        AdminLessonDetailDto dto = new(
            lesson.Id,
            lesson.Title,
            lesson.Description,
            lesson.Type,
            lesson.Level,
            lesson.TargetAgeGroup,
            lesson.IsPublished,
            lesson.VocabularyWords
                .Where(link => link.Sense is not null)
                .OrderBy(link => link.SortOrder)
                .Select(link => new AdminLessonSenseDto(
                    link.SenseId,
                    link.Sense!.Word,
                    link.Sense.Definition,
                    link.Sense.Example,
                    link.SortOrder,
                    !link.Sense.IsHiddenFromChildrenInLessons,
                    link.Sense.ShareStatus,
                    link.Sense.Source))
                .ToList());

        _logger.LogInformation("GetAdminLessonDetailQuery succeeded: LessonId={LessonId}", lesson.Id);
        return Result.Success(dto);
    }
}
