using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Interfaces;

/// <summary>
/// Persists groups and members. <c>Add*</c> and tracked reads only stage changes;
/// <see cref="SaveChangesAsync"/> commits them (and any outbox rows) in one transaction.
/// Deleted groups are never returned.
/// </summary>
public interface ILearnerGroupRepository
{
    /// <summary>Returns the tracked group, or <see cref="LearnerGroupErrors.NotFound"/>.</summary>
    Task<Result<LearnerGroup>> GetGroupTrackedAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Returns the group (read-only), or <see cref="LearnerGroupErrors.NotFound"/>.</summary>
    Task<Result<LearnerGroup>> GetGroupAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Returns groups by id (read-only).</summary>
    Task<IReadOnlyList<LearnerGroup>> GetGroupsByIdsAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken ct = default);

    /// <summary>Counts the groups the owner has.</summary>
    Task<int> CountGroupsByOwnerAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>Returns groups the user owns (read-only).</summary>
    Task<IReadOnlyList<LearnerGroup>> GetGroupsByOwnerAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>Returns the pending and active members of the groups (read-only).</summary>
    Task<IReadOnlyList<LearnerGroupMember>> GetOpenMembersAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken ct = default);

    /// <summary>Returns the pending and active members of one group (tracked).</summary>
    Task<IReadOnlyList<LearnerGroupMember>> GetOpenMembersTrackedAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>Returns the active memberships of the learner (read-only).</summary>
    Task<IReadOnlyList<LearnerGroupMember>> GetActiveMembershipsOfLearnerAsync(Guid learnerId, CancellationToken ct = default);

    /// <summary>Returns the tracked member, or <see cref="LearnerGroupErrors.MemberNotFound"/>.</summary>
    Task<Result<LearnerGroupMember>> GetMemberTrackedAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Returns the tracked pending or active member of the learner in the group, or not found.</summary>
    Task<Result<LearnerGroupMember>> GetOpenMemberTrackedAsync(Guid groupId, Guid learnerId, CancellationToken ct = default);

    /// <summary>Returns the tracked pending or active memberships of the learner in groups owned by the owner.</summary>
    Task<IReadOnlyList<LearnerGroupMember>> GetOpenMembersOfOwnerAndLearnerTrackedAsync(Guid ownerId, Guid learnerId, CancellationToken ct = default);

    /// <summary>Returns pending members whose learner has <paramref name="primarySupporterId"/> as active Primary (read-only).</summary>
    Task<IReadOnlyList<LearnerGroupMember>> GetPendingForPrimaryAsync(Guid primarySupporterId, CancellationToken ct = default);

    /// <summary>Stages a new group.</summary>
    Task AddGroupAsync(LearnerGroup group, CancellationToken ct = default);

    /// <summary>Stages a new member.</summary>
    Task AddMemberAsync(LearnerGroupMember member, CancellationToken ct = default);

    /// <summary>Commits all staged changes in one transaction.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct = default);
}
