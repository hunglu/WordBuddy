namespace WordBuddy.Application.DTOs;

/// <summary>Learner progress representation for API responses.</summary>
public sealed record LearnerProgressDto(
    Guid Id,
    Guid UserId,
    Guid LessonId,
    bool IsCompleted,
    int? ScorePercent,
    DateTime LastAccessedAt,
    DateTime? CompletedAt);
