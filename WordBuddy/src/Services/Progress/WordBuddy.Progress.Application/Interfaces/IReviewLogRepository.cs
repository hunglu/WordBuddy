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

    /// <summary>
    /// Logs before <paramref name="endsAtUtc"/> of the given learners (read-only): every log inside
    /// [<paramref name="startsAtUtc"/>, <paramref name="endsAtUtc"/>) plus the earlier logs of the words answered in that window.
    /// Used by the challenge score projection.
    /// </summary>
    Task<IReadOnlyList<ReviewLog>> GetForChallengeAsync(IReadOnlyCollection<Guid> userIds, DateTime startsAtUtc, DateTime endsAtUtc, CancellationToken ct = default);

    /// <summary>Counts earlier correct answers for this word in this session.</summary>
    Task<Result<int>> CountCorrectAsync(Guid userId, Guid sessionId, Guid senseId, CancellationToken ct = default);
}
