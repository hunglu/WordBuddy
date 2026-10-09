using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Store for <see cref="VocabularySessionIssue"/>. Rows are never deleted; <see cref="VocabularySessionIssue.End"/> is the only change.</summary>
public interface IVocabularySessionIssueRepository
{
    /// <summary>Stages a new row. Committed by <see cref="ILearnerWordStateRepository.SaveChangesAsync"/>.</summary>
    Task<Result> AddAsync(VocabularySessionIssue issue, CancellationToken ct = default);

    /// <summary>
    /// The user's latest change-tracked session that is not ended and not expired at
    /// <paramref name="nowUtc"/>, with its items. <see cref="ErrorType.NotFound"/> if none.
    /// </summary>
    Task<Result<VocabularySessionIssue>> GetOpenAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>Sense ids the user has answered in the session (from the review log).</summary>
    Task<Result<IReadOnlySet<Guid>>> GetAnsweredSenseIdsAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
}
