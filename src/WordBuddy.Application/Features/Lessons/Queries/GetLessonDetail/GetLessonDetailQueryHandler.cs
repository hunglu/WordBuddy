using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Features.Lessons.Queries.GetLessonDetail;

/// <summary>Assembles a full lesson detail by fetching the lesson and all related content items.</summary>
public sealed class GetLessonDetailQueryHandler
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IVocabularyItemRepository _vocabularyItemRepository;
    private readonly IGrammarRuleRepository _grammarRuleRepository;
    private readonly IDailyPhraseRepository _dailyPhraseRepository;
    private readonly IValidator<GetLessonDetailQuery> _validator;
    private readonly ILogger<GetLessonDetailQueryHandler> _logger;

    /// <summary>Initializes a new <see cref="GetLessonDetailQueryHandler"/>.</summary>
    public GetLessonDetailQueryHandler(
        ILessonRepository lessonRepository,
        IVocabularyItemRepository vocabularyItemRepository,
        IGrammarRuleRepository grammarRuleRepository,
        IDailyPhraseRepository dailyPhraseRepository,
        IValidator<GetLessonDetailQuery> validator,
        ILogger<GetLessonDetailQueryHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _vocabularyItemRepository = vocabularyItemRepository;
        _grammarRuleRepository = grammarRuleRepository;
        _dailyPhraseRepository = dailyPhraseRepository;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Handles the query and returns the full lesson detail including all content items.</summary>
    public async Task<Result<LessonDetailDto>> HandleAsync(GetLessonDetailQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetLessonDetailQuery started: LessonId={LessonId}", query.LessonId);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "GetLessonDetailQuery validation failed: {Errors}", validation.ToString());
            return Result<LessonDetailDto>.Failure(
                Error.Validation("GetLessonDetail.Validation", validation.ToString()));
        }

        Result<Lesson> lessonResult = await _lessonRepository.GetByIdAsync(query.LessonId, ct);
        if (lessonResult.IsFailure)
        {
            _logger.LogWarning(
                "GetLessonDetailQuery lesson fetch failed: LessonId={LessonId}, {ErrorCode} — {ErrorDescription}",
                query.LessonId, lessonResult.Error.Code, lessonResult.Error.Description);
            return Result<LessonDetailDto>.Failure(lessonResult.Error);
        }

        // Start all three fetches concurrently; tasks are in-flight before any await.
        Task<Result<IReadOnlyList<VocabularyItem>>> vocabTask =
            _vocabularyItemRepository.GetByLessonIdAsync(query.LessonId, ct);
        Task<Result<IReadOnlyList<GrammarRule>>> grammarTask =
            _grammarRuleRepository.GetByLessonIdAsync(query.LessonId, ct);
        Task<Result<IReadOnlyList<DailyPhrase>>> phraseTask =
            _dailyPhraseRepository.GetByLessonIdAsync(query.LessonId, ct);

        Result<IReadOnlyList<VocabularyItem>> vocabResult   = await vocabTask;
        Result<IReadOnlyList<GrammarRule>>    grammarResult = await grammarTask;
        Result<IReadOnlyList<DailyPhrase>>    phraseResult  = await phraseTask;

        if (vocabResult.IsFailure)
        {
            _logger.LogWarning(
                "GetLessonDetailQuery vocabulary fetch failed: LessonId={LessonId}, {ErrorCode} — {ErrorDescription}",
                query.LessonId, vocabResult.Error.Code, vocabResult.Error.Description);
            return Result<LessonDetailDto>.Failure(vocabResult.Error);
        }
        if (grammarResult.IsFailure)
        {
            _logger.LogWarning(
                "GetLessonDetailQuery grammar fetch failed: LessonId={LessonId}, {ErrorCode} — {ErrorDescription}",
                query.LessonId, grammarResult.Error.Code, grammarResult.Error.Description);
            return Result<LessonDetailDto>.Failure(grammarResult.Error);
        }
        if (phraseResult.IsFailure)
        {
            _logger.LogWarning(
                "GetLessonDetailQuery phrase fetch failed: LessonId={LessonId}, {ErrorCode} — {ErrorDescription}",
                query.LessonId, phraseResult.Error.Code, phraseResult.Error.Description);
            return Result<LessonDetailDto>.Failure(phraseResult.Error);
        }

        Lesson lesson = lessonResult.Value;

        LessonDetailDto dto = new(
            lesson.Id, lesson.Title, lesson.Description, lesson.Type, lesson.Level,
            lesson.TargetAgeGroup, lesson.IsPublished, lesson.OrderIndex, lesson.CreatedAt,
            VocabularyItems: vocabResult.Value.Select(v => new VocabularyItemDto(
                v.Id, v.LessonId, v.Word, v.Definition, v.ExampleSentence,
                v.PhoneticSpelling, v.ImageAssetId, v.AudioAssetId)).ToList(),
            GrammarRules: grammarResult.Value.Select(g => new GrammarRuleDto(
                g.Id, g.LessonId, g.RuleName, g.Explanation, g.OrderIndex, g.Examples)).ToList(),
            DailyPhrases: phraseResult.Value.Select(p => new DailyPhraseDto(
                p.Id, p.LessonId, p.Phrase, p.Meaning, p.UsageContext,
                p.AudioAssetId, p.VideoAssetId)).ToList());

        return Result<LessonDetailDto>.Success(dto);
    }
}
