using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.DTOs;

/// <summary>Full lesson representation including all associated content items.</summary>
public sealed record LessonDetailDto(
    Guid Id,
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    TargetAgeGroup TargetAgeGroup,
    bool IsPublished,
    int OrderIndex,
    DateTime CreatedAt,
    IReadOnlyList<VocabularyItemDto> VocabularyItems,
    IReadOnlyList<GrammarRuleDto> GrammarRules,
    IReadOnlyList<DailyPhraseDto> DailyPhrases);
