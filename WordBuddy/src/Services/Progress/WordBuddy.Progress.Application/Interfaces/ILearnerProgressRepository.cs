using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

public interface ILearnerProgressRepository
{
    /// <summary>Returns all progress entries for <paramref name="userId"/>.</summary>
    Task<Result<IReadOnlyList<LearnerProgress>>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns a change-tracked entity for <paramref name="userId"/>/<paramref name="lessonId"/>
    /// if one exists, or <see cref="Error.NotFound"/> — tracked so the caller can mutate it via
    /// <see cref="LearnerProgress.RecordCompletion"/> and persist with <see cref="SaveChangesAsync"/>.
    /// </summary>
    Task<Result<LearnerProgress>> GetTrackedByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);

    Task<Result> AddAsync(LearnerProgress progress, CancellationToken ct = default);

    /// <summary>Persists changes made to an entity previously returned by <see cref="GetTrackedByUserAndLessonAsync"/>.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
