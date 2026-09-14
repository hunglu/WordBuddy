using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetLessonDetail;

public sealed class GetLessonDetailQueryHandler : IQueryHandler<GetLessonDetailQuery, LessonDetailDto>
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<GetLessonDetailQueryHandler> _logger;

    public GetLessonDetailQueryHandler(ILessonRepository lessonRepository, ILogger<GetLessonDetailQueryHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _logger = logger;
    }

    public async Task<Result<LessonDetailDto>> HandleAsync(GetLessonDetailQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetLessonDetailQuery started: LessonId={LessonId}", query.LessonId);

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdWithDetailsAsync(query.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "GetLessonDetailQuery repository failure: {ErrorCode} — {ErrorDescription}",
                lessonResult.Error.Code, lessonResult.Error.Description);
            return Result.Failure<LessonDetailDto>(lessonResult.Error);
        }

        Lesson lesson = lessonResult.Value;

        LessonDetailDto dto = new(
            lesson.Id,
            lesson.Title,
            lesson.Description,
            lesson.Type,
            lesson.Level,
            lesson.TargetAgeGroup,
            lesson.VocabularyItems
                .Select(v => new VocabularyItemDto(v.Id, v.Word, v.Definition, v.Example, ToDto(v.Audio)))
                .ToList(),
            lesson.GrammarRules
                .Select(g => new GrammarRuleDto(g.Id, g.Title, g.Explanation, g.Examples))
                .ToList(),
            lesson.DailyPhrases
                .Select(d => new DailyPhraseDto(d.Id, d.Phrase, d.Translation, ToDto(d.Audio), ToDto(d.Video)))
                .ToList());

        _logger.LogInformation("GetLessonDetailQuery succeeded: LessonId={LessonId}", lesson.Id);
        return Result.Success(dto);
    }

    private static MediaAssetDto? ToDto(MediaAsset? asset) =>
        asset is null ? null : new MediaAssetDto(asset.Id, asset.Type, asset.Url);
}
