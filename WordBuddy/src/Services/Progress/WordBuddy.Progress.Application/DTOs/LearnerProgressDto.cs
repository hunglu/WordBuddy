namespace WordBuddy.Progress.Application.DTOs;

public sealed record LearnerProgressDto(Guid Id, Guid LessonId, bool IsCompleted, int? ScorePercent, DateTime? CompletedAtUtc);
