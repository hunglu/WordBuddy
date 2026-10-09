using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>Stores <see cref="LearnerWordState"/> rows — one per (user, sense). Writes are staged
/// and committed by <see cref="SaveChangesAsync"/> together with any staged review log.</summary>
public interface ILearnerWordStateRepository
{
    /// <summary>Returns the change-tracked state (active or not), or <see cref="ErrorType.NotFound"/>.</summary>
    Task<Result<LearnerWordState>> GetTrackedAsync(Guid userId, Guid senseId, CancellationToken ct = default);

    /// <summary>Stages a new state. Not saved until <see cref="SaveChangesAsync"/>.</summary>
    Task<Result> AddAsync(LearnerWordState state, CancellationToken ct = default);

    /// <summary>Active, already-reviewed states due at or before <paramref name="nowUtc"/>, earliest first.</summary>
    Task<Result<IReadOnlyList<LearnerWordState>>> GetDueAsync(Guid userId, DateTime nowUtc, int take, CancellationToken ct = default);

    /// <summary>Counts active, already-reviewed states due at or before <paramref name="nowUtc"/>.</summary>
    Task<Result<int>> CountDueAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>Counts states first reviewed in [<paramref name="fromUtc"/>, <paramref name="toUtc"/>).</summary>
    Task<Result<int>> CountFirstReviewedBetweenAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>Active never-reviewed states, oldest membership first.</summary>
    Task<Result<IReadOnlyList<LearnerWordState>>> GetNewInAddedOrderAsync(Guid userId, int take, CancellationToken ct = default);

    /// <summary>All active states of the user.</summary>
    Task<Result<IReadOnlyList<LearnerWordState>>> GetActiveAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Active states of the user for the given sense ids.</summary>
    Task<Result<IReadOnlyList<LearnerWordState>>> GetActiveBySenseIdsAsync(Guid userId, IReadOnlyCollection<Guid> senseIds, CancellationToken ct = default);

    /// <summary>Commits every staged change of this scope in one transaction.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
