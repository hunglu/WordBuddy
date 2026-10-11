using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Stores the local <see cref="ChallengeProfile"/> copies.</summary>
public interface IChallengeProfileRepository
{
    /// <summary>Returns the profile (read-only), or not found.</summary>
    Task<Result<ChallengeProfile>> GetAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns the tracked profile, or not found.</summary>
    Task<Result<ChallengeProfile>> GetTrackedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Profiles of the given users (read-only). Missing ids are skipped.</summary>
    Task<IReadOnlyList<ChallengeProfile>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Stages a new profile.</summary>
    Task AddAsync(ChallengeProfile profile, CancellationToken ct = default);

    /// <summary>Saves staged changes. A concurrent first insert returns a conflict (the retry policy re-runs).</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
