namespace WordBuddy.Application.DTOs;

/// <summary>Vocabulary word representation for API responses.</summary>
public sealed record VocabularyItemDto(
    Guid Id,
    Guid LessonId,
    string Word,
    string Definition,
    string ExampleSentence,
    string? PhoneticSpelling,
    Guid? ImageAssetId,
    Guid? AudioAssetId);
