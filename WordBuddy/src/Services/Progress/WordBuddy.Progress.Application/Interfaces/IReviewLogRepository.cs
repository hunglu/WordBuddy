using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Insert-only store for <see cref="ReviewLog"/>. No update or delete by design.</summary>
public interface IReviewLogRepository
{
    /// <summary>Stages a new row. Committed by <see cref="ILearnerWordStateRepository.SaveChangesAsync"/>.</summary>
    Task<Result> AddAsync(ReviewLog log, CancellationToken ct = default);

    /// <summary>Counts earlier answers for this word in this session.</summary>
    Task<Result<int>> CountAttemptsAsync(Guid userId, Guid sessionId, Guid senseId, CancellationToken ct = default);

    /// <summary>All logs of the given learners (read-only). Used by the challenge score projection.</summary>
    Task<IReadOnlyList<ReviewLog>> GetByUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Counts earlier correct answers for this word in this session.</summary>
    Task<Result<int>> CountCorrectAsync(Guid userId, Guid sessionId, Guid senseId, CancellationToken ct = default);
}
