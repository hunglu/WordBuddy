namespace WordBuddy.Application.DTOs;

/// <summary>Grammar rule representation for API responses.</summary>
public sealed record GrammarRuleDto(
    Guid Id,
    Guid LessonId,
    string RuleName,
    string Explanation,
    int OrderIndex,
    IReadOnlyList<string> Examples);
