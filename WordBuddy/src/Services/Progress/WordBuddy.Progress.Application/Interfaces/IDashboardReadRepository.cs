using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Interfaces;

/// <summary>One answer, projected to the columns the dashboard needs.</summary>
public sealed record DashboardReviewRow(
    Guid SenseId,
    Guid SessionId,
    DateTime OccurredAtUtc,
    VocabularySkill Skill,
    bool IsCorrect,
    int ResponseMs,
    bool HintUsed,
    bool IsDue,
    int AttemptNo);

/// <summary>An active word state, projected.</summary>
public sealed record DashboardWordRow(Guid SenseId, WordStatus Status, int Lapses);

/// <summary>An active word membership, projected.</summary>
public sealed record DashboardMembershipRow(Guid SenseId, Guid AddedBy, DateTime AddedAtUtc);

/// <summary>An issued session, projected.</summary>
public sealed record DashboardSessionRow(Guid SessionId, DateTime IssuedAtUtc, int PlannedCount, DateTime ExpiresAtUtc);

/// <summary>Read-only queries for the dashboard. Projected columns, no tracking.</summary>
public interface IDashboardReadRepository
{
    /// <summary>Answers with <c>OccurredAtUtc</c> in [<paramref name="fromUtc"/>, <paramref name="toUtc"/>).</summary>
    Task<Result<IReadOnlyList<DashboardReviewRow>>> GetReviewLogsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>All active word states of the learner.</summary>
    Task<Result<IReadOnlyList<DashboardWordRow>>> GetWordStatesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Active memberships added in [<paramref name="fromUtc"/>, <paramref name="toUtc"/>).</summary>
    Task<Result<IReadOnlyList<DashboardMembershipRow>>> GetMembershipsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>Sessions issued in [<paramref name="fromUtc"/>, <paramref name="toUtc"/>).</summary>
    Task<Result<IReadOnlyList<DashboardSessionRow>>> GetSessionIssuesAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}
