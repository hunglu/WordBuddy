namespace WordBuddy.Domain.Entities;

/// <summary>Tracks a learner's access, completion, and score for a specific lesson.</summary>
public sealed class LearnerProgress
{
    /// <summary>Initializes a new <see cref="LearnerProgress"/>.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="userId">Identifier of the learner.</param>
    /// <param name="lessonId">Identifier of the lesson.</param>
    /// <param name="isCompleted">Whether the learner has completed the lesson.</param>
    /// <param name="scorePercent">Quiz score as a percentage (0–100); <see langword="null"/> if the lesson has no quiz.</param>
    /// <param name="lastAccessedAt">UTC timestamp when the learner last accessed this lesson.</param>
    /// <param name="completedAt">UTC timestamp when the learner completed the lesson; <see langword="null"/> if not yet completed.</param>
    public LearnerProgress(
        Guid id,
        Guid userId,
        Guid lessonId,
        bool isCompleted,
        int? scorePercent,
        DateTime lastAccessedAt,
        DateTime? completedAt)
    {
        Id = id;
        UserId = userId;
        LessonId = lessonId;
        IsCompleted = isCompleted;
        ScorePercent = scorePercent;
        LastAccessedAt = lastAccessedAt;
        CompletedAt = completedAt;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the identifier of the learner.</summary>
    public Guid UserId { get; }

    /// <summary>Gets the identifier of the lesson.</summary>
    public Guid LessonId { get; }

    /// <summary>Gets a value indicating whether the learner has completed the lesson.</summary>
    public bool IsCompleted { get; }

    /// <summary>Gets the quiz score as a percentage (0–100), or <see langword="null"/> if the lesson has no quiz.</summary>
    public int? ScorePercent { get; }

    /// <summary>Gets the UTC timestamp when the learner last accessed this lesson.</summary>
    public DateTime LastAccessedAt { get; }

    /// <summary>Gets the UTC timestamp when the learner completed the lesson, or <see langword="null"/> if not yet completed.</summary>
    public DateTime? CompletedAt { get; }
}
