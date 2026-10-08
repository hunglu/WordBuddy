using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Interfaces;

/// <summary>
/// Persists support links, invitations, unlink requests and audit rows. <c>Add*</c> and tracked
/// reads only stage changes; <see cref="SaveChangesAsync"/> commits them (and any outbox rows)
/// in one transaction.
/// </summary>
public interface ISupportLinkRepository
{
    /// <summary>Returns the tracked link, or <see cref="SupportLinkErrors.LinkNotFound"/>.</summary>
    Task<Result<SupportLink>> GetLinkTrackedAsync(Guid linkId, CancellationToken ct = default);

    /// <summary>Returns all tracked links of a learner (any status).</summary>
    Task<IReadOnlyList<SupportLink>> GetLearnerLinksTrackedAsync(Guid learnerId, CancellationToken ct = default);

    /// <summary>Returns all links where the user is learner or supporter (read-only).</summary>
    Task<IReadOnlyList<SupportLink>> GetLinksForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns links of a learner (read-only, admin view).</summary>
    Task<IReadOnlyList<SupportLink>> GetLearnerLinksAsync(Guid learnerId, CancellationToken ct = default);

    /// <summary>Whether the learner has at least one active supporter.</summary>
    Task<bool> HasActiveSupporterAsync(Guid learnerId, CancellationToken ct = default);

    /// <summary>Returns the active Primary supporter of the learner, if any.</summary>
    Task<Guid?> GetActivePrimarySupporterIdAsync(Guid learnerId, CancellationToken ct = default);

    /// <summary>Returns links waiting for approval by <paramref name="primarySupporterId"/> (read-only).</summary>
    Task<IReadOnlyList<SupportLink>> GetPendingForPrimaryAsync(Guid primarySupporterId, CancellationToken ct = default);

    /// <summary>Stages a new link.</summary>
    Task AddLinkAsync(SupportLink link, CancellationToken ct = default);

    /// <summary>Returns the tracked invitation with this code or token hash, or not found.</summary>
    Task<Result<SupportLinkInvitation>> GetInvitationByHashTrackedAsync(string? codeHash, string? tokenHash, CancellationToken ct = default);

    /// <summary>Returns the tracked invitation, or not found.</summary>
    Task<Result<SupportLinkInvitation>> GetInvitationTrackedAsync(Guid invitationId, CancellationToken ct = default);

    /// <summary>Returns pending, not expired invitations created by the user (read-only).</summary>
    Task<IReadOnlyList<SupportLinkInvitation>> GetOpenInvitationsByCreatorAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>Stages a new invitation.</summary>
    Task AddInvitationAsync(SupportLinkInvitation invitation, CancellationToken ct = default);

    /// <summary>Returns the tracked open unlink request of the link, or <see cref="SupportLinkErrors.UnlinkRequestNotFound"/>.</summary>
    Task<Result<UnlinkRequest>> GetOpenUnlinkRequestTrackedAsync(Guid linkId, CancellationToken ct = default);

    /// <summary>Returns the tracked unlink request, or not found.</summary>
    Task<Result<UnlinkRequest>> GetUnlinkRequestTrackedAsync(Guid unlinkRequestId, CancellationToken ct = default);

    /// <summary>Returns open unlink requests of the given links (read-only).</summary>
    Task<IReadOnlyList<UnlinkRequest>> GetOpenUnlinkRequestsAsync(IReadOnlyCollection<Guid> linkIds, CancellationToken ct = default);

    /// <summary>Returns unlink requests escalated to an admin (read-only).</summary>
    Task<IReadOnlyList<UnlinkRequest>> GetEscalatedUnlinkRequestsAsync(CancellationToken ct = default);

    /// <summary>Returns links by id (read-only).</summary>
    Task<IReadOnlyList<SupportLink>> GetLinksByIdsAsync(IReadOnlyCollection<Guid> linkIds, CancellationToken ct = default);

    /// <summary>Stages a new unlink request.</summary>
    Task AddUnlinkRequestAsync(UnlinkRequest request, CancellationToken ct = default);

    /// <summary>Stages an audit row.</summary>
    Task AddAuditEntryAsync(SupportLinkAuditEntry entry, CancellationToken ct = default);

    /// <summary>Commits all staged changes in one transaction.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs <paramref name="work"/> (which may call <see cref="SaveChangesAsync"/> several times) in one
    /// database transaction. Commits only when <paramref name="work"/> succeeds.
    /// </summary>
    Task<Result> ExecuteInTransactionAsync(Func<CancellationToken, Task<Result>> work, CancellationToken ct = default);
}
