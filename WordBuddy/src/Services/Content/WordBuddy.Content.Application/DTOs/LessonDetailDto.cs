using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

public sealed record LessonDetailDto(
    Guid Id,
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    AgeGroup TargetAgeGroup,
    IReadOnlyList<VocabularyItemDto> VocabularyItems,
    IReadOnlyList<GrammarRuleDto> GrammarRules,
    IReadOnlyList<DailyPhraseDto> DailyPhrases);
