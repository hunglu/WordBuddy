using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
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

        // Same error as an unknown id, so a caller cannot probe for drafts or adult-only lessons.
        bool hiddenFromCaller = !lesson.IsPublished ||
            (query.RequestingAgeGroup == AgeGroup.Child && lesson.TargetAgeGroup != AgeGroup.Child);
        if (hiddenFromCaller)
        {
            _logger.LogWarning(
                "GetLessonDetailQuery lesson not visible: LessonId={LessonId}, IsPublished={IsPublished}, AgeGroup={AgeGroup}",
                lesson.Id, lesson.IsPublished, query.RequestingAgeGroup);
            return Result.Failure<LessonDetailDto>(Error.NotFound("Lesson.NotFound", $"Lesson {query.LessonId} was not found."));
        }

        LessonDetailDto dto = new(
            lesson.Id,
            lesson.Title,
            lesson.Description,
            lesson.Type,
            lesson.Level,
            lesson.TargetAgeGroup,
            // The JSON stays "vocabularyItems" with the same shape; words now come through
            // LessonSenses. System words always carry a non-empty Example.
            lesson.VocabularyWords
                .Where(link => link.Sense is not null)
                .Where(link => IsListedFor(link.Sense!, query.RequestingAgeGroup))
                .OrderBy(link => link.SortOrder)
                .Select(link => link.Sense!)
                .Select(v => ToVocabularyItem(v, query.RequestingAgeGroup))
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

    /// <summary>A Child does not get words hidden from children. An unapproved auto-filled word stays
    /// listed, as an "awaiting approval" placeholder without content (WB-25).</summary>
    private static bool IsListedFor(Sense sense, AgeGroup ageGroup) =>
        ageGroup != AgeGroup.Child ||
        !sense.IsHiddenFromChildrenInLessons ||
        sense.IsAwaitingChildApproval(ageGroup, callerLink: null);

    /// <summary>Lesson word with WB-25 enrichment. A Child waiting for approval of an auto-filled
    /// word gets no content.</summary>
    private static VocabularyItemDto ToVocabularyItem(Sense v, AgeGroup ageGroup)
    {
        if (v.IsAwaitingChildApproval(ageGroup, callerLink: null))
        {
            return VocabularyItemDto.WithDetails(
                new VocabularyItemDto(v.Id, v.Word, string.Empty, string.Empty, null), SenseDetails.Empty(v.Origin)) with
            {
                AwaitingApproval = true,
            };
        }

        return VocabularyItemDto.WithDetails(
            new VocabularyItemDto(v.Id, v.Word, v.Definition, v.Example ?? string.Empty, ToDto(v.Audio)), SenseDetails.From(v));
    }

    private static MediaAssetDto? ToDto(MediaAsset? asset) =>
        asset is null ? null : new MediaAssetDto(asset.Id, asset.Type, asset.Url);
}
