using WordBuddy.Domain.Common;
using WordBuddy.Domain.Entities;

namespace WordBuddy.Application.Interfaces;

/// <summary>Repository contract for <see cref="LearnerProgress"/> entities.</summary>
public interface ILearnerProgressRepository : IRepository<LearnerProgress>
{
    /// <summary>Retrieves all progress records for the specified user.</summary>
    Task<Result<IReadOnlyList<LearnerProgress>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the progress record for a specific user–lesson pair.
    /// Returns <see cref="Error.NotFound"/> with code <c>LearnerProgress.NotFound</c> when no record exists yet.
    /// </summary>
    Task<Result<LearnerProgress>> GetByUserAndLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);
}
