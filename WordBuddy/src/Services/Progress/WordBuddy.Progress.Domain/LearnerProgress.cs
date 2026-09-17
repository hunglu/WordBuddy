using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>Tracks one user's completion and score for one lesson. <see cref="UserId"/> and
/// <see cref="LessonId"/> are plain fields, not foreign keys — Progress has no reference to
/// Identity's or Content's data, consistent with the independence model.</summary>
public sealed class LearnerProgress : Entity
{
    public Guid UserId { get; }
    public Guid LessonId { get; }
    public bool IsCompleted { get; private set; }
    public int? ScorePercent { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public LearnerProgress(Guid id, Guid userId, Guid lessonId) : base(id)
    {
        UserId = userId;
        LessonId = lessonId;
    }

    /// <summary>Records (or re-records, on retake) completion for this lesson.</summary>
    public void RecordCompletion(bool isCompleted, int? scorePercent)
    {
        IsCompleted = isCompleted;
        ScorePercent = scorePercent;
        CompletedAtUtc = isCompleted ? DateTime.UtcNow : null;
    }
}
