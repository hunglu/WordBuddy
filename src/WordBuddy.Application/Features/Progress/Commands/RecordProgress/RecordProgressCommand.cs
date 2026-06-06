namespace WordBuddy.Application.Features.Progress.Commands.RecordProgress;

/// <summary>Command to record or update a learner's progress on a lesson.</summary>
/// <param name="UserId">The learner's identifier.</param>
/// <param name="LessonId">The lesson being accessed or completed.</param>
/// <param name="IsCompleted">Whether the learner has completed the lesson.</param>
/// <param name="ScorePercent">Quiz score 0–100; <see langword="null"/> when the lesson has no quiz.</param>
public sealed record RecordProgressCommand(
    Guid UserId,
    Guid LessonId,
    bool IsCompleted,
    int? ScorePercent);
