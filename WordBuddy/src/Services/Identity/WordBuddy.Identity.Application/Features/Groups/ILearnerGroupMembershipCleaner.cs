namespace WordBuddy.Identity.Application.Features.Groups;

/// <summary>
/// Removes a learner from every group of one supporter when their support link ends. Stages the
/// changes and outbox events only; the caller commits them in the same save as the link change.
/// </summary>
public interface ILearnerGroupMembershipCleaner
{
    /// <summary>
    /// Sets the pending and active memberships of <paramref name="learnerId"/> in groups owned by
    /// <paramref name="supporterId"/> to Removed (LinkRevoked) and stages <c>LearnerGroupMemberRemoved</c>
    /// for each active one. Returns the affected group ids, so the caller can drop their cache keys after saving.
    /// </summary>
    Task<IReadOnlyList<Guid>> RemoveForLinkAsync(Guid learnerId, Guid supporterId, DateTime nowUtc, CancellationToken ct = default);
}
