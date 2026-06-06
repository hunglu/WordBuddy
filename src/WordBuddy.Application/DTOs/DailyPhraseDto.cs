namespace WordBuddy.Application.DTOs;

/// <summary>Daily phrase representation for API responses.</summary>
public sealed record DailyPhraseDto(
    Guid Id,
    Guid LessonId,
    string Phrase,
    string Meaning,
    string UsageContext,
    Guid? AudioAssetId,
    Guid? VideoAssetId);
