namespace WordBuddy.Progress.Application.DTOs;

public sealed record VocabularyRecallProgressDto(
    int TotalWordsTracked,
    int KnownCount,
    int LearningCount,
    IReadOnlyList<VocabularyRecallSessionDto> RecentSessions);
